using Mecanica.Hermes.Domain.Common.Abstractions;

namespace Mecanica.Hermes.Application.Common.Persistence;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken);
    void CollectEventsFrom(AggregateRoot aggregate);
}