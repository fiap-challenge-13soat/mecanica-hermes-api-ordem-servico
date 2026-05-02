using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mecanica.Hermes.Infrastructure.HealthChecks;

/// <summary>
///     Faz ping na instância MongoDB usada para persistir o estado da SAGA.
///     Cacheia um <see cref="MongoClient" /> por connection string, pois MongoClient
///     é thread-safe e foi projetado para ser um singleton de vida longa.
/// </summary>
[ExcludeFromCodeCoverage(
    Justification = "Health check de infraestrutura — depende de MongoDB real e é validado em runtime.")]
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
