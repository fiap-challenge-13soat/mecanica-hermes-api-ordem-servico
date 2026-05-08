using System.Diagnostics;
using System.Text.Json;
using Mecanica.Hermes.Infrastructure.Persistence.Outbox;
using Mecanica.Hermes.Shared.Core.Domain;
using Mecanica.Hermes.Shared.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mecanica.Hermes.Infrastructure.Persistence.UnitOfWork;

/// <summary>
///     Unit of Work baseado em EF Core com <b>Outbox transacional</b>.
///     Estratégia (Outbox Pattern):
///     <list type="number">
///         <item>Domain Events são coletados pelo agregado via <see cref="CollectEventsFrom" />.</item>
///         <item>
///             No <see cref="CommitAsync" />, cada evento é serializado em JSON e adicionado como
///             <see cref="OutboxMessageEntity" /> na <em>mesma</em> transação do <c>SaveChangesAsync</c>.
///             Isso garante atomicidade: ou o estado e os eventos são gravados, ou nada é.
///         </item>
///         <item>
///             Um <see cref="OutboxProcessor" /> em background lê mensagens não processadas e
///             despacha via <see cref="IDomainEventDispatcher" />.
///         </item>
///     </list>
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
                try
                {
                    var payload = JsonSerializer.Serialize(evt, evt.GetType(), SerializerOptions);
                    var evtType = evt.GetType();
                    var nameAttr =
                        (DomainEventNameAttribute?)Attribute.GetCustomAttribute(evtType,
                            typeof(DomainEventNameAttribute));
                    var typeName = nameAttr?.Name ?? evtType.AssemblyQualifiedName ?? evtType.FullName!;
                    var traceContext = Activity.Current?.Id;
                    db.OutboxMessages.Add(new OutboxMessageEntity(
                        Guid.NewGuid(),
                        typeName,
                        payload,
                        now,
                        traceContext));
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

        try
        {
            return await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Encapsula a exceção de concorrência do EF Core para que a camada Application
            // permaneça livre do EF Core. A política de retry do MassTransit reentregará a
            // mensagem e o consumer recarregará o agregado com o estado atual antes de tentar novamente.
            throw new ConcurrencyConflictException(
                "Conflito de concorrência ao salvar a Ordem de Serviço. O registro foi modificado por outra operação.",
                ex);
        }
    }
}
