using MassTransit;
using Mecanica.Hermes.Infrastructure.MassTransit.Configurations;
using Mecanica.Hermes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Mecanica.Hermes.IntegrationTests.Infrastructure;

public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;

    static IntegrationTestWebApplicationFactory()
    {
        // Disable RabbitMQ for integration tests - use in-memory transport instead
        Environment.SetEnvironmentVariable("MASSTRANSIT__TRANSPORT_SERVICE", "None");
    }

    public IntegrationTestWebApplicationFactory()
    {
        _dbContainer = new PostgreSqlBuilder("postgres:18.1")
            .WithDatabase("mecanica_hermes_test")
            .WithUsername("postgres")
            .WithPassword("teste123A!")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start the container and wait for it to be ready
        await _dbContainer.StartAsync();

        // Set the environment variable with the TestContainer connection string
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _dbContainer.GetConnectionString());

        // Create a test client to ensure the app is built with the correct connection string
        _ = CreateClient();

        // Run migrations after container is started and app is built
        try
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Ensure database is created and migrations are applied
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to run migrations. Connection String: {_dbContainer.GetConnectionString()}. " +
                $"Error: {ex.Message}",
                ex);
        }
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set required auth environment variables for Testing
        Environment.SetEnvironmentVariable("AUTH__AUTHORITY", "https://test-authority");
        Environment.SetEnvironmentVariable("AUTH__ISSUER", "https://test-issuer");

        builder.UseEnvironment("Testing");

        // Override the connection string after the container is started
        builder.ConfigureServices(services =>
        {
            // MassTransit em modo in-memory para testes:
            // - SAGA com InMemoryRepository (mongoConnectionString = null aciona esse modo)
            // - Todos os Consumers da Application registrados
            // - Transport in-memory (substitui RabbitMq do composition root real)
            services.AddMassTransit(x =>
            {
                x.ConfigureSaga(null, "test");
                x.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); });
            });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
