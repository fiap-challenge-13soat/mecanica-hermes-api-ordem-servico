using System.Diagnostics;
using System.Text.Json;
using Mecanica.Hermes.Application.Common.Exceptions;
using Mecanica.Hermes.Application.Common.Persistence;
using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mecanica.Hermes.Infrastructure.Persistence.UnitOfWork;

/// <summary>
/// Unit of Work baseado em EF Core com <b>Outbox transacional</b>.
///
/// Estratégia (Outbox Pattern):
/// <list type="number">
///   <item>Domain Events são coletados pelo agregado via <see cref="CollectEventsFrom"/>.</item>
///   <item>No <see cref="CommitAsync"/>, cada evento é serializado em JSON e adicionado como
///   <see cref="OutboxMessageEntity"/> na <em>mesma</em> transação do <c>SaveChangesAsync</c>.
///   Isso garante atomicidade: ou o estado e os eventos são gravados, ou nada é.</item>
///   <item>Um <see cref="OutboxProcessor"/> em background lê mensagens não processadas e
///   despacha via <see cref="IDomainEventDispatcher"/>.</item>
/// </list>
///
/// Esta abordagem substitui o despacho síncrono pós-commit (que perdia eventos em falha
/// de dispatch). Agora a entrega é eventualmente consistente e at-least-once.
/// </summary>
internal sealed class EfUnitOfWork(
    AppDbContext db,
    ILogger<EfUnitOfWork> logger,
    TimeProvider timeProvider) : IUnitOfWork
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private readonly List<IDomainEvent> _pendingEvents = [];

    public void CollectEventsFrom(AggregateRoot aggregate)
    {
        _pendingEvents.AddRange(aggregate.DomainEvents);
        aggregate.ClearDomainEvents();
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken)
    {
        if (_pendingEvents.Count > 0)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var snapshot = _pendingEvents.ToArray();
            _pendingEvents.Clear();

            foreach (var evt in snapshot)
            {
                try
                {
                    var payload = JsonSerializer.Serialize(evt, evt.GetType(), SerializerOptions);
                    var evtType = evt.GetType();
                    var nameAttr = (DomainEventNameAttribute?)Attribute.GetCustomAttribute(evtType, typeof(DomainEventNameAttribute));
                    var typeName = nameAttr?.Name ?? evtType.AssemblyQualifiedName ?? evtType.FullName!;
                    var traceContext = Activity.Current?.Id;
                    db.OutboxMessages.Add(new OutboxMessageEntity(
                        id: Guid.NewGuid(),
                        type: typeName,
                        payload: payload,
                        occurredOn: now,
                        traceContext: traceContext));
                }
                catch (Exception ex)
                {
                    // Serialização falhou — log e segue sem outbox para esse evento.
                    // Decisão deliberada: a perda de um evento isolado é preferível a abortar o commit
                    // do estado. Casos críticos devem fazer o serializador ser totalmente determinístico.
                    logger.LogError(ex,
                        "Falha ao serializar Domain Event {EventType} para Outbox. Evento NÃO será despachado.",
                        evt.GetType().Name);
                }
            }
        }

        try
        {
            return await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Wrap EF Core concurrency exception so Application layer stays free of EF Core.
            // MassTransit's retry policy will redeliver the message and the consumer will
            // reload the aggregate with the current state before trying again.
            throw new ConcurrencyConflictException(
                "Conflito de concorrência ao salvar a Ordem de Serviço. O registro foi modificado por outra operação.",
                ex);
        }
    }
}
