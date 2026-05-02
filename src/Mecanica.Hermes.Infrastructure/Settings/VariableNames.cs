namespace Mecanica.Hermes.Infrastructure.Settings;

internal static class VariableNames
{
    public const string MediatRLicenseKey = "MediatR__LicenseKey";
    public const string DefaultConnectionString = "ConnectionStrings__DefaultConnection";
    public const string AuthAuthority = "AUTH__AUTHORITY";
    public const string AuthIssuer = "AUTH__ISSUER";
    public const string AuthClienteScope = "AUTH__CLIENTE_SCOPE";
    public const string AuthAdminScope = "AUTH__ADMIN_SCOPE";
    public const string ObservabilityEnabled = "OBSERVABILITY__ENABLED";

    // Especificação OpenTelemetry — identificação do serviço.
    public const string OtelServiceName = "OTEL_SERVICE_NAME";

    // Especificação OpenTelemetry — env vars genéricas do exporter OTLP.
    public const string OtelExporterOtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
    public const string OtelExporterOtlpHeaders = "OTEL_EXPORTER_OTLP_HEADERS";
    public const string OtelExporterOtlpProtocol = "OTEL_EXPORTER_OTLP_PROTOCOL";

    // Especificação OpenTelemetry — overrides específicos do sinal de logs.
    public const string OtelExporterOtlpLogsEndpoint = "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT";
    public const string OtelExporterOtlpLogsHeaders = "OTEL_EXPORTER_OTLP_LOGS_HEADERS";
    public const string OtelExporterOtlpLogsProtocol = "OTEL_EXPORTER_OTLP_LOGS_PROTOCOL";

    // Especificação do MassTransit
    public const string MassTransitConnectionString = "MASSTRANSIT__CONNECTION_STRING";
    public const string MassTransitPrefetchCount = "MASSTRANSIT__PREFETCH_COUNT";
    public const string MassTransitTransportService = "MASSTRANSIT__TRANSPORT_SERVICE";
    public const string MassTransitQueueNamePrefix = "MASSTRANSIT__QUEUE_NAME_PREFIX";

    // Especificação do MongoDB — persistência do estado da SAGA
    public const string MongoDbConnectionString = "MONGODB__CONNECTION_STRING";
    public const string MongoDbSagaDatabase = "MONGODB__SAGA_DATABASE";

    // Controle de migrations
    public const string RunMigrationsOnStartup = "RUN_MIGRATIONS_ON_STARTUP";
}
