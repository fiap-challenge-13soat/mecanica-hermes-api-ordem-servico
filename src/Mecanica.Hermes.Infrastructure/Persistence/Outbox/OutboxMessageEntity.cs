namespace Mecanica.Hermes.Infrastructure.Persistence.Outbox;

/// <summary>
/// Estado de ciclo de vida de uma <see cref="OutboxMessageEntity"/>.
/// </summary>
internal enum OutboxMessageStatus
{
    /// <summary>Aguardando despacho pelo <see cref="OutboxProcessor"/>.</summary>
    Pending = 0,

    /// <summary>Despachada com sucesso. Candidata à rotina de cleanup de retenção.</summary>
    Processed = 1,

    /// <summary>
    /// Excedeu o número máximo de tentativas (<see cref="OutboxMessageEntity.DefaultMaxRetries"/>).
    /// Não será reprocessada automaticamente; requer intervenção manual ou pipeline de
    /// forensics.
    /// </summary>
    DeadLettered = 2
}

/// <summary>
/// Mensagem persistida no padrão Outbox transacional. Cada Domain Event é serializado
/// e gravado na mesma transação do <c>SaveChangesAsync</c> do agregado, garantindo
/// consistência entre estado e despacho. O <see cref="OutboxProcessor"/> em background
/// lê mensagens com <c>Status = Pending</c> e as despacha via <c>IDomainEventDispatcher</c>.
/// </summary>
internal sealed class OutboxMessageEntity
{
    /// <summary>
    /// Número máximo de tentativas antes de a mensagem ser movida para <see cref="OutboxMessageStatus.DeadLettered"/>.
    /// Configurável via env var <c>OUTBOX__MAX_RETRIES</c> (lida em startup pelo OutboxOptions).
    /// </summary>
    public const int DefaultMaxRetries = 10;

    private OutboxMessageEntity()
    {
        // EF Constructor
        Type = null!;
        Payload = null!;
    }

    public OutboxMessageEntity(Guid id, string type, string payload, DateTime occurredOn, string? traceContext = null)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredOn = occurredOn;
        TraceContext = traceContext;
        Status = OutboxMessageStatus.Pending;
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// Discriminador de tipo do evento. Usa <c>DomainEventNameAttribute</c> quando
    /// presente; caso contrário, cai de volta para <c>AssemblyQualifiedName</c>.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>JSON serializado do evento.</summary>
    public string Payload { get; private set; }

    /// <summary>Timestamp de ocorrência do evento (UTC do commit).</summary>
    public DateTime OccurredOn { get; private set; }

    /// <summary>Estado atual da mensagem no ciclo de vida do Outbox.</summary>
    public OutboxMessageStatus Status { get; private set; }

    /// <summary>Quando <see cref="Status"/> == <see cref="OutboxMessageStatus.Processed"/>, instante do despacho.</summary>
    public DateTime? ProcessedOn { get; private set; }

    /// <summary>Quantidade de tentativas de despacho já realizadas.</summary>
    public int RetryCount { get; private set; }

    /// <summary>Última mensagem de erro registrada (preservada mesmo após dead-letter).</summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// W3C <c>traceparent</c> capturado no momento do commit. Permite reconstruir a cadeia de
    /// trace entre a request originadora e o despacho assíncrono do evento.
    /// </summary>
    public string? TraceContext { get; private set; }

    public void MarkAsProcessed(DateTime when)
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedOn = when;
        LastError = null;
    }

    public void MarkAsFailed(string error, int maxRetries = DefaultMaxRetries)
    {
        RetryCount++;
        LastError = error;

        if (RetryCount >= maxRetries)
            Status = OutboxMessageStatus.DeadLettered;
    }
}
