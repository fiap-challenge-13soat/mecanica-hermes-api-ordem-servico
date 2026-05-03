using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MassTransit;
using Mecanica.Hermes.Infrastructure.MassTransit.Configurations;
using Mecanica.Hermes.Infrastructure.Persistence;
using Mecanica.Hermes.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace Mecanica.Hermes.IntegrationTests.Infrastructure;

/// <summary>
///     Factory para testes de autorização. Configura JWT real com chave simétrica fixa
///     e NÃO usa <c>DevelopmentAuthenticationMiddleware</c> (ambiente é "Testing_Auth",
///     não "Testing"), permitindo testar 401/403 com tokens reais ou ausência de token.
/// </summary>
public sealed class AuthorizationTestWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    internal const string TestIssuer = "https://test-issuer";

    // Estes valores coincidem com o fallback de AuthPolicies ("admin-scope" / "cliente-scope").
    // AuthPolicies usa campos static readonly inicializados no carregamento da classe —
    // a primeira factory (IntegrationTestWebApplicationFactory) carrega a classe antes que
    // estas env vars sejam definidas, então os valores de fallback se tornam os nomes
    // efetivos das policies para todo o ciclo de vida do processo. Os tokens devem carregar
    // os mesmos valores.
    internal const string AdminScope = "admin-scope";
    internal const string ClientScope = "cliente-scope";

    private static readonly SymmetricSecurityKey TestSigningKey =
        new(Encoding.UTF8.GetBytes("test-signing-key-must-be-long-enough-for-hs256-algorithm!"));

    static AuthorizationTestWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("MASSTRANSIT__TRANSPORT_SERVICE", "None");
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Usa um nome de ambiente diferente para evitar o DevelopmentAuthenticationMiddleware
        // (Program.cs só o adiciona quando IsDevelopment() || EnvironmentName == "Testing").
        builder.UseEnvironment("Testing_Auth");

        // Garante uma connection string não nula para que o NpgsqlDataSourceBuilder não falhe na inicialização.
        // O banco real é substituído por InMemory abaixo via ConfigureServices.
        // Só define se ainda não estiver populado — evita sobrescrever a URL real do container
        // que IntegrationTestWebApplicationFactory possa ter definido ao rodar no mesmo processo.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(VariableNames.DefaultConnectionString)))
            Environment.SetEnvironmentVariable(VariableNames.DefaultConnectionString,
                "Host=localhost;Port=5432;Database=placeholder;Username=test;Password=test");
        Environment.SetEnvironmentVariable("AUTH__AUTHORITY", TestIssuer);
        Environment.SetEnvironmentVariable("AUTH__ISSUER", TestIssuer);
        Environment.SetEnvironmentVariable("RUN_MIGRATIONS_ON_STARTUP", "false");
        Environment.SetEnvironmentVariable("OUTBOX__ALLOW_NON_LOCKING_FALLBACK", "true");

        builder.ConfigureServices(services =>
        {
            // Remove o data source do Npgsql e as DbContextOptions existentes.
            // NOTA: remover apenas isso NÃO basta — UseNpgsql chama internamente
            // NpgsqlOptionsExtension.ApplyServices, que registra vários serviços do
            // Npgsql EF Core no container da app. UseInMemoryDatabase faz o mesmo, então
            // o EF Core veria dois providers e lançaria erro. A solução é UseInternalServiceProvider:
            // damos ao EF Core um service provider isolado para que ele nunca inspecione
            // o container da app em busca de conflitos de provider.
            services.RemoveAll<NpgsqlDataSource>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<AppDbContext>();

            var efInMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<AppDbContext>(opt =>
                opt.UseInMemoryDatabase("auth-tests-db-" + Guid.NewGuid())
                    .UseInternalServiceProvider(efInMemoryProvider));

            // Sobrescreve as opções JWT para validar com nossa chave simétrica de teste (sem JWKS).
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = TestIssuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = TestSigningKey,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddMassTransit(x =>
            {
                x.ConfigureSaga(null, "test");
                // Habilita o MessageSchedulerContext no pipe — sem isso as atividades
                // `.Schedule(...)` da SAGA falham com PayloadNotFoundException.
                // Espelha o setup de produção (RabbitMqBusFactoryConfiguratorExtensions).
                x.AddDelayedMessageScheduler();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseDelayedMessageScheduler();
                    cfg.ConfigureEndpoints(context);
                });
            });
        });
    }

    internal static string GenerateToken(IEnumerable<string> scopes)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "test-user"),
            new(JwtRegisteredClaimNames.Iss, TestIssuer),
            new("scope", string.Join(" ", scopes))
        };

        var credentials = new SigningCredentials(TestSigningKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            TestIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
