using Mecanica.Hermes.Domain.Common.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Mecanica.Hermes.Infrastructure.Persistence.Events;

/// <summary>
/// Despacha cada Domain Event como uma <see cref="DomainEventNotification{T}"/> via MediatR.
/// Isolamento por evento: uma falha em um handler é capturada, logada e a iteração continua,
/// para que um evento "tóxico" não impeça os demais — o Result Pattern já capturou os erros
/// de domínio antes do dispatch, então qualquer exceção aqui é infra/handler.
/// </summary>
internal sealed class DomainEventDispatcher(
    IPublisher publisher,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        foreach (var e in events)
        {
            try
            {
                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(e.GetType());
                var notification = (INotification)Activator.CreateInstance(notificationType, e)!;
                await publisher.Publish(notification, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Falha ao despachar Domain Event {EventType}. Continuando com os demais.",
                    e.GetType().Name);
            }
        }
    }
}
