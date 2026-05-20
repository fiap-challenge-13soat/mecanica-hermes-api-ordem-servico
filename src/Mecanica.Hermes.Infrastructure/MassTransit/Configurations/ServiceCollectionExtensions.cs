using MassTransit;
using Mecanica.Hermes.Infrastructure.MassTransit.Observability;
using Mecanica.Hermes.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using static System.Int32;

namespace Mecanica.Hermes.Infrastructure.MassTransit.Configurations;

internal static class ServiceCollectionExtensions
{
    private static int _mongoConventionsRegistered;

    public static void ConfigureMassTransit(this IServiceCollection services)
    {
        EnsureMongoConventions();
        services.AddMassTransitOptions();
        services.AddSingleton<DlqMetrics>();
        services.AddSingleton<MessagingFaultObserver>();

        var transportService = ResolveTransportService();

        switch (transportService)
        {
            case TransportService.RabbitMq:
                services.AddMassTransitWithRabbitMq();
                break;
            case TransportService.AmazonMq:
                services.AddMassTransitWithAmazonMq();
                break;
        }
    }

    // Default = AmazonMq quando a env var está ausente ou inválida. Mantém `None`
    // explícito como escape hatch para testes de integração que não querem broker.
    private static TransportService ResolveTransportService()
    {
        var raw = Environment.GetEnvironmentVariable(VariableNames.MassTransitTransportService);
        if (string.IsNullOrWhiteSpace(raw))
            return TransportService.AmazonMq;

        return Enum.TryParse<TransportService>(raw, ignoreCase: true, out var parsed)
            ? parsed
            : TransportService.AmazonMq;
    }

    // MongoDB.Driver 3.x exige GuidRepresentation explicito. Sem isto, o MassTransit
    // MongoDb saga repository falha ao serializar OperationTimeoutTokenId (Guid?) em
    // OrdemDeServicoState com BsonSerializationException - mensagens vao para DLQ e
    // a SAGA nunca persiste.
    private static void EnsureMongoConventions()
    {
        if (Interlocked.Exchange(ref _mongoConventionsRegistered, 1) == 1) return;
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    private static void AddMassTransitWithRabbitMq(this IServiceCollection services)
    {
        var mongoConnectionString = Environment.GetEnvironmentVariable(VariableNames.MongoDbConnectionString);
        var mongoDatabaseName = Environment.GetEnvironmentVariable(VariableNames.MongoDbSagaDatabase)
                                ?? MassTransitOptions.DefaultMongoDbSagaDatabase;
        services.AddMassTransit(x =>
        {
            // Registra o health check do bus com as tags "ready" e "messaging" para que seja
            // incluído em /healthz/ready. Usa Degraded (e não Unhealthy) porque o MassTransit
            // tenta reconectar — o pod deve permanecer no pool durante quedas breves do broker
            // em vez de falhar imediatamente o readiness probe.
            x.ConfigureHealthCheckOptions(options =>
            {
                options.MinimalFailureStatus = HealthStatus.Degraded;
                options.Tags.Add("ready");
                options.Tags.Add("messaging");
            });

            x.ConfigureSaga(mongoConnectionString, mongoDatabaseName);
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.ConfigureRabbitMq(context);
            });
        });
    }

    private static void AddMassTransitWithAmazonMq(this IServiceCollection services)
    {
        var mongoConnectionString = Environment.GetEnvironmentVariable(VariableNames.MongoDbConnectionString);
        var mongoDatabaseName = Environment.GetEnvironmentVariable(VariableNames.MongoDbSagaDatabase)
                                ?? MassTransitOptions.DefaultMongoDbSagaDatabase;
        services.AddMassTransit(x =>
        {
            x.ConfigureHealthCheckOptions(options =>
            {
                options.MinimalFailureStatus = HealthStatus.Degraded;
                options.Tags.Add("ready");
                options.Tags.Add("messaging");
            });

            x.ConfigureSaga(mongoConnectionString, mongoDatabaseName);
            x.UsingRabbitMq((context, cfg) => cfg.ConfigureAmazonMq(context));
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
            options.MassTransitConnectionString =
                Environment.GetEnvironmentVariable(VariableNames.MassTransitConnectionString);
            options.MassTransitPrefetchCount = prefetchCount > 0 ? prefetchCount : 24;
            options.QueueNamePrefix = queueNamePrefix;
            options.MongoDbConnectionString = mongoConnectionString;
            options.MongoDbSagaDatabaseName = mongoDatabaseName;
        });

        services.AddSingleton<IEndpointNameFormatter>(
            new KebabCaseEndpointNameFormatter(queueNamePrefix, false));
    }
}
