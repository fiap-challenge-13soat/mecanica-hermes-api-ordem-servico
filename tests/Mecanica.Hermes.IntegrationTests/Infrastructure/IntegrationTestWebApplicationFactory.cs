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
        // Desabilita RabbitMQ nos testes de integração — usa o transporte in-memory.
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
        // Inicia o container e aguarda ficar pronto.
        await _dbContainer.StartAsync();

        // Define a variável de ambiente com a connection string do TestContainer.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _dbContainer.GetConnectionString());

        // Cria um cliente de teste para garantir que a app seja construída com a connection string correta.
        _ = CreateClient();

        // Executa as migrations após o container estar iniciado e a app construída.
        try
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Garante que o banco esteja criado e as migrations aplicadas.
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
        // Define as variáveis de ambiente de autenticação exigidas em Testing.
        Environment.SetEnvironmentVariable("AUTH__AUTHORITY", "https://test-authority");
        Environment.SetEnvironmentVariable("AUTH__ISSUER", "https://test-issuer");

        builder.UseEnvironment("Testing");

        // Sobrescreve a connection string depois que o container é iniciado.
        builder.ConfigureServices(services =>
        {
            // MassTransit em modo in-memory para testes:
            // - SAGA com InMemoryRepository (mongoConnectionString = null aciona esse modo)
            // - Todos os Consumers da Application registrados
            // - Transport in-memory (substitui RabbitMq do composition root real)
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

    public override async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
