using MassTransit;
using Mecanica.Hermes.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using static System.Int32;

namespace Mecanica.Hermes.Infrastructure.MassTransit.Configurations;

internal static class ServiceCollectionExtensions
{
    public static void ConfigureMassTransit(this IServiceCollection services)
    {
        services.AddMassTransitOptions();
        _ = Enum.TryParse<TransportService>(Environment.GetEnvironmentVariable(VariableNames.MassTransitTransportService), out var transportService);

        if (transportService == TransportService.RabbitMq)
            services.AddMassTransitWithRabbitMq();
    }

    private static void AddMassTransitWithRabbitMq(this IServiceCollection services)
    {
        var mongoConnectionString = Environment.GetEnvironmentVariable(VariableNames.MongoDbConnectionString);
        var mongoDatabaseName = Environment.GetEnvironmentVariable(VariableNames.MongoDbSagaDatabase)
            ?? MassTransitOptions.DefaultMongoDbSagaDatabase;

        services.AddMassTransit(x =>
        {
            // Register the bus health check with "ready" and "messaging" tags so it is
            // included in /healthz/ready. Degraded (not Unhealthy) because MassTransit
            // retries the connection — the pod should stay in the pool during brief broker
            // outages rather than immediately failing the readiness probe.
            x.ConfigureHealthCheckOptions(options =>
            {
                options.MinimalFailureStatus = HealthStatus.Degraded;
                options.Tags.Add("ready");
                options.Tags.Add("messaging");
            });

            x.ConfigureSaga(mongoConnectionString, mongoDatabaseName);
            x.UsingRabbitMq((context, cfg) => cfg.ConfigureRabbitMq(context));
        });
    }
    
    private static void AddMassTransitOptions(this IServiceCollection services)
    {
        _ = TryParse(Environment.GetEnvironmentVariable(VariableNames.MassTransitPrefetchCount), out var prefetchCount);

        var queueNamePrefix = Environment.GetEnvironmentVariable(VariableNames.MassTransitQueueNamePrefix);
        if (string.IsNullOrWhiteSpace(queueNamePrefix))
            queueNamePrefix = MassTransitOptions.DefaultQueueNamePrefix;

        var mongoConnectionString = Environment.GetEnvironmentVariable(VariableNames.MongoDbConnectionString);
        var mongoDatabaseName = Environment.GetEnvironmentVariable(VariableNames.MongoDbSagaDatabase)
            ?? MassTransitOptions.DefaultMongoDbSagaDatabase;
        
        services.Configure<MassTransitOptions>(options =>
        {
            options.MassTransitConnectionString = Environment.GetEnvironmentVariable(VariableNames.MassTransitConnectionString);
            options.MassTransitPrefetchCount = prefetchCount;
            options.QueueNamePrefix = queueNamePrefix;
            options.MongoDbConnectionString = mongoConnectionString;
            options.MongoDbSagaDatabaseName = mongoDatabaseName;
        });

        services.AddSingleton<IEndpointNameFormatter>(
            new KebabCaseEndpointNameFormatter(queueNamePrefix, false));
    }
}