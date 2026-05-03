using Mecanica.Hermes.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mecanica.Hermes.Infrastructure.HealthChecks;

public static class InfrastructureHealthChecksExtensions
{
    /// <summary>
    ///     Adiciona um health check de MongoDB (<c>mongodb-saga</c>) quando
    ///     <c>MONGODB__CONNECTION_STRING</c> está definida. No-op quando a variável está ausente
    ///     (ex.: SAGA in-memory / testes).
    /// </summary>
    /// <remarks>
    ///     O próprio health check de bus do MassTransit (conectividade RabbitMQ / Amazon SQS) é
    ///     registrado automaticamente pelo <c>AddMassTransit</c> via
    ///     <c>ConfigureHealthCheckOptions</c> no setup do MassTransit —
    ///     nenhuma chamada explícita é necessária aqui.
    /// </remarks>
    public static IHealthChecksBuilder AddSagaStorageHealthCheck(
        this IHealthChecksBuilder builder,
        HealthStatus failureStatus = HealthStatus.Degraded,
        string[]? tags = null)
    {
        var mongoConnectionString = Environment.GetEnvironmentVariable(VariableNames.MongoDbConnectionString);
        if (string.IsNullOrWhiteSpace(mongoConnectionString))
            return builder;

        return builder.Add(new HealthCheckRegistration(
            "mongodb-saga",
            _ => new MongoDbSagaHealthCheck(mongoConnectionString),
            failureStatus,
            tags ?? []));
    }
}
