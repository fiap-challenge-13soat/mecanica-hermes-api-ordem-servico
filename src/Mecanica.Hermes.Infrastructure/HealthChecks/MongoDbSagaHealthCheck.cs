using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mecanica.Hermes.Infrastructure.HealthChecks;

/// <summary>
///     Pings the MongoDB instance used for SAGA state persistence.
///     Caches one <see cref="MongoClient" /> per connection string because MongoClient
///     is thread-safe and intended to be a long-lived singleton.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Health check de infraestrutura — depende de MongoDB real e é validado em runtime.")]
internal sealed class MongoDbSagaHealthCheck(string connectionString) : IHealthCheck
{
    private static readonly ConcurrentDictionary<string, IMongoClient> _clients = new();

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clients.GetOrAdd(connectionString, cs => new MongoClient(cs));
            var db = client.GetDatabase("admin");
            await db.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
