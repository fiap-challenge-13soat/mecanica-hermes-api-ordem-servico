namespace Mecanica.Hermes.Infrastructure.MassTransit.Configurations;

internal class MassTransitOptions
{
    public const string DefaultQueueNamePrefix = "mecanica-hermes";
    public const string DefaultMongoDbSagaDatabase = "mechermes_saga";

    public string? MassTransitConnectionString { get; set; }
    public int MassTransitPrefetchCount { get; set; } = 24;
    public string QueueNamePrefix { get; set; } = DefaultQueueNamePrefix;
    public string? MongoDbConnectionString { get; set; }
    public string MongoDbSagaDatabaseName { get; set; } = DefaultMongoDbSagaDatabase;
}