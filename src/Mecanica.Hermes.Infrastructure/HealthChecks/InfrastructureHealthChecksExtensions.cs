using Mecanica.Hermes.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mecanica.Hermes.Infrastructure.HealthChecks;

public static class InfrastructureHealthChecksExtensions
{
    /// <summary>
    ///     Adds a MongoDB health check (<c>mongodb-saga</c>) when
    ///     <c>MONGODB__CONNECTION_STRING</c> is set. No-op when the variable is absent
    ///     (e.g. in-memory SAGA / tests).
    /// </summary>
    /// <remarks>
    ///     MassTransit's own bus health check (RabbitMQ / Amazon SQS connectivity) is
    ///     registered automatically by <c>AddMassTransit</c> via
    ///     <c>ConfigureHealthCheckOptions</c> inside the MassTransit setup —
    ///     no explicit call is needed here.
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
