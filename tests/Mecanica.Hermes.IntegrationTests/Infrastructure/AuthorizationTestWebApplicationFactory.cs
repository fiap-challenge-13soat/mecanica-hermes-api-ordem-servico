using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mecanica.Hermes.Infrastructure.MassTransit.Configurations;
using Mecanica.Hermes.Infrastructure.Persistence;
using Mecanica.Hermes.Infrastructure.Settings;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace Mecanica.Hermes.IntegrationTests.Infrastructure;

/// <summary>
/// Factory para testes de autorização. Configura JWT real com chave simétrica fixa
/// e NÃO usa <c>DevelopmentAuthenticationMiddleware</c> (ambiente é "Testing_Auth",
/// não "Testing"), permitindo testar 401/403 com tokens reais ou ausência de token.
/// </summary>
public sealed class AuthorizationTestWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly SymmetricSecurityKey TestSigningKey =
        new(System.Text.Encoding.UTF8.GetBytes("test-signing-key-must-be-long-enough-for-hs256-algorithm!"));

    internal const string TestIssuer = "https://test-issuer";
    // These values match the AuthPolicies fallback ("admin-scope" / "cliente-scope").
    // AuthPolicies uses static readonly fields that are initialized at class-load time —
    // the first factory (IntegrationTestWebApplicationFactory) loads the class before these
    // env vars are set, so the fallback values become the effective policy names for the
    // entire process lifetime. Tokens must carry the same values.
    internal const string AdminScope = "admin-scope";
    internal const string ClientScope = "cliente-scope";

    static AuthorizationTestWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("MASSTRANSIT__TRANSPORT_SERVICE", "None");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use a different environment name to avoid DevelopmentAuthenticationMiddleware
        // (Program.cs only adds it when IsDevelopment() || EnvironmentName == "Testing").
        builder.UseEnvironment("Testing_Auth");

        // Ensure a non-null connection string so NpgsqlDataSourceBuilder does not crash at startup.
        // The real DB is replaced by InMemory below via ConfigureServices.
        // Only set if not already populated — avoids overwriting the real container URL
        // that IntegrationTestWebApplicationFactory may have set when running in the same process.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(VariableNames.DefaultConnectionString)))
        {
            Environment.SetEnvironmentVariable(VariableNames.DefaultConnectionString,
                "Host=localhost;Port=5432;Database=placeholder;Username=test;Password=test");
        }
        Environment.SetEnvironmentVariable("AUTH__AUTHORITY", TestIssuer);
        Environment.SetEnvironmentVariable("AUTH__ISSUER", TestIssuer);
        Environment.SetEnvironmentVariable("RUN_MIGRATIONS_ON_STARTUP", "false");
        Environment.SetEnvironmentVariable("OUTBOX__ALLOW_NON_LOCKING_FALLBACK", "true");

        builder.ConfigureServices(services =>
        {
            // Remove the Npgsql data source and existing DbContext options.
            // NOTE: removing these alone is NOT enough — UseNpgsql internally calls
            // NpgsqlOptionsExtension.ApplyServices which registers many Npgsql EF Core
            // services into the app container. UseInMemoryDatabase does the same, so EF
            // Core would see two providers and throw. The fix is UseInternalServiceProvider:
            // we give EF Core a dedicated, isolated service provider so it never inspects
            // the app container for provider conflicts.
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

            // Override JWT options to validate with our test symmetric key (no JWKS needed)
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
                x.ConfigureSaga(mongoConnectionString: null, mongoDatabaseName: "test");
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
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
            issuer: TestIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
