using System.Net;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Mecanica.Hermes.Api;
using Mecanica.Hermes.Api.Endpoints.OrdensDeServico;
using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;
using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Validators;
using Mecanica.Hermes.Api.Middleware;
using Mecanica.Hermes.Infrastructure.DependencyInjection;
using Mecanica.Hermes.Infrastructure.HealthChecks;
using Mecanica.Hermes.Infrastructure.Settings;
using Mecanica.Hermes.Shared.AspNetCore;
using Mecanica.Hermes.Shared.AspNetCore.Middleware;
using Mecanica.Hermes.Shared.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
using IPNetwork = System.Net.IPNetwork;

var builder = WebApplication.CreateSlimBuilder(args);

if (builder.Environment.EnvironmentName != EnvironmentNames.Testing)
    builder.AddSerilogConfiguration();

builder.Services.ConfigureInfrastructureServices(typeof(IApiAssemblyMarker).Assembly);

// Registra os validators do FluentValidation (usados por ValidationFilter<T>).
builder.Services.AddSingleton<IValidator<CreateOrdemDeServicoRequest>, CreateOrdemDeServicoRequestValidator>();
builder.Services.AddSingleton<IValidator<AddOrdemDeServicoProdutoRequest>, AddOrdemDeServicoProdutoRequestValidator>();
builder.Services.AddSingleton<IValidator<AddOrdemDeServicoServicoRequest>, AddOrdemDeServicoServicoRequestValidator>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options =>
{
    var publicUrl = Environment.GetEnvironmentVariable("PUBLIC_URL");
    if (!string.IsNullOrEmpty(publicUrl))
        options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Servers = [new() { Url = publicUrl.TrimEnd('/') }];
            return Task.CompletedTask;
        });
});

// CORS: política nomeada "default". Em DEV/Testing libera Any para acelerar testes manuais
// no Scalar/Swagger; em Production exige a env var CORS_ALLOWED_ORIGINS (CSV) — sem ela,
// não há nenhuma origem permitida (fail-secure).
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        if (builder.Environment.IsDevelopment() ||
            builder.Environment.EnvironmentName == EnvironmentNames.Testing)
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        var origins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

// ForwardedHeaders: necessário para que o rate limiter leia o IP real do cliente quando
// a API roda atrás de um ingress/proxy (nginx, Kubernetes ingress, AWS ALB etc.).
// FORWARDED_HEADERS_KNOWN_PROXIES: CSV de IPs/CIDRs dos proxies confiáveis (ex: "10.0.0.0/8,192.168.1.1").
// Em Development/Testing limpa as listas (aceita qualquer proxy); em produção usa os valores da env var.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var knownProxiesRaw = Environment.GetEnvironmentVariable("FORWARDED_HEADERS_KNOWN_PROXIES") ?? string.Empty;
    var knownProxies = knownProxiesRaw
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (builder.Environment.IsDevelopment() ||
        builder.Environment.EnvironmentName == EnvironmentNames.Testing ||
        knownProxies.Length == 0)
    {
        // Aceita X-Forwarded-For de qualquer origem — adequado para DEV/Testing
        // e para ambientes onde o CIDR do ingress não é fixo.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
    else
    {
        foreach (var entry in knownProxies)
            if (entry.Contains('/') &&
                IPNetwork.TryParse(entry, out var network))
                options.KnownIPNetworks.Add(network);
            else if (IPAddress.TryParse(entry, out var ip))
                options.KnownProxies.Add(ip);
    }
});

// Rate limiting: protege endpoints de mutação. Limite por IP usando fixed window
// (60 requests/min). Usa X-Forwarded-For via UseForwardedHeaders para IP real atrás de proxy.
// Em Testing o limite é int.MaxValue para evitar flakiness em testes de integração.
var isTesting = builder.Environment.EnvironmentName == EnvironmentNames.Testing;
var rlFromEnv = int.TryParse(Environment.GetEnvironmentVariable("RATE_LIMIT_PERMIT_PER_MINUTE"), out var p) ? p : 60;
var rlPermit = isTesting ? int.MaxValue : rlFromEnv;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rlPermit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgres",
        failureStatus: HealthStatus.Unhealthy,
        tags: [HealthCheckTags.Ready, HealthCheckTags.Database])
    // O check do bus do MassTransit (RabbitMQ / Amazon SQS) é registrado automaticamente
    // pelo AddMassTransit via ConfigureHealthCheckOptions no setup do Infrastructure.
    .AddSagaStorageHealthCheck(
        HealthStatus.Degraded,
        [HealthCheckTags.Ready, HealthCheckTags.Saga]);

builder.Host.ConfigureHostBuilder();

var app = builder.Build();

if (builder.Environment.EnvironmentName != EnvironmentNames.Testing)
    app.UseSerilogConfiguration();

// Deve ser o primeiro middleware do pipeline para que o RemoteIpAddress
// já reflita o IP real antes do rate limiter e dos logs de acesso.
app.UseForwardedHeaders();

app.UseCorrelationId();
app.UseGlobalExceptionHandler();
app.UseCors("default");
app.UseRateLimiter();
app.MapOpenApi("/ordem-servico/openapi/v1.json");
app.MapScalarApiReference("ordem-servico/scalar", options =>
{
    options.WithOpenApiRoutePattern("/ordem-servico/openapi/v1.json");
});
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == EnvironmentNames.Testing)
{
    app.UseDevelopmentAuthentication(new DevelopmentAuthenticationOptions
    {
        Scopes = [AuthPolicies.OnlyAdminScope, AuthPolicies.AllowClienteScope]
    });
    app.Logger.LogWarning(
        "DevelopmentAuthenticationMiddleware ENABLED — environment: {Environment}. " +
        "All unauthenticated requests will be granted full access. " +
        "This middleware must NEVER run in production.",
        app.Environment.EnvironmentName);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = _ => false // apenas o check "self" (sempre saudável)
});
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = hc => hc.Tags.Contains(HealthCheckTags.Ready)
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = hc => hc.Tags.Contains(HealthCheckTags.Ready)
});

app.MapOrdensDeServicoEndpoints();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

lifetime.ApplicationStarted.Register(() =>
    logger.LogInformation("Aplicação iniciada com sucesso."));

await app.MigrateDatabaseAsync();

await app.RunAsync();
