using System.Reflection;
using Mecanica.Hermes.Application;
using Mecanica.Hermes.Application.Common.Behaviors;
using Mecanica.Hermes.Shared.Application.Behaviors;
using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain;
using Mecanica.Hermes.Infrastructure.MassTransit.Adapters;
using Mecanica.Hermes.Infrastructure.MassTransit.Configurations;
using Mecanica.Hermes.Infrastructure.Observability;
using Mecanica.Hermes.Infrastructure.Persistence.DependencyInjection;
using Mecanica.Hermes.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Mecanica.Hermes.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void ConfigureInfrastructureServices(this IServiceCollection services, Assembly apiAssemblyMarker)
    {
        services.AddEntityFrameworkConfiguration();
        services.AddObservability();
        services.ConfigureMassTransit();

        services.AddSingleton<OrdemDeServicoMetrics>();
        services.AddSingleton<IOrdemDeServicoMetrics>(sp => sp.GetRequiredService<OrdemDeServicoMetrics>());

        services.AddScoped<IAsyncMessagePublisher, MassTransitMessagePublisher>();

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = Environment.GetEnvironmentVariable(VariableNames.MediatRLicenseKey);
            cfg.RegisterServicesFromAssemblies(apiAssemblyMarker);
            cfg.RegisterServicesFromAssemblyContaining<IApplicationAssemblyMarker>();
            cfg.RegisterServicesFromAssemblyContaining<IDomainAssemblyMarker>();
            cfg.RegisterServicesFromAssemblyContaining<IInfrastructureAssemblyMarker>();
            cfg.RegisterGenericHandlers = true;
            cfg.AddOpenBehavior(typeof(TracingBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = Environment.GetEnvironmentVariable(VariableNames.AuthAuthority);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Environment.GetEnvironmentVariable(VariableNames.AuthIssuer),
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.AllowClienteScope, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var scopeClaims = context.User.FindAll("scope");
                    return scopeClaims.Any(scopeClaim =>
                    {
                        var scopes = scopeClaim.Value
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        return scopes.Contains(AuthPolicies.AllowClienteScope) ||
                               scopes.Contains(AuthPolicies.OnlyAdminScope);
                    });
                });
            })
            .AddPolicy(AuthPolicies.OnlyAdminScope, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var scopeClaims = context.User.FindAll("scope");
                    return scopeClaims.Any(scopeClaim =>
                    {
                        var scopes = scopeClaim.Value
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        return scopes.Contains(AuthPolicies.OnlyAdminScope);
                    });
                });
            });
    }
}
