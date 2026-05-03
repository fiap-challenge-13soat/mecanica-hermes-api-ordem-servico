namespace Mecanica.Hermes.Infrastructure.Persistence.Outbox;

/// <summary>
///     Opções de comportamento do <see cref="OutboxProcessor" />, configuráveis via env vars ou
///     <c>appsettings.json</c> (seção <c>Outbox</c>).
/// </summary>
internal sealed class OutboxOptions
{
    /// <summary>Intervalo entre polls do background processor. Default: 2s.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Tamanho máximo do lote por ciclo. Default: 50.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    ///     Número máximo de tentativas antes de mover para
    ///     <see cref="OutboxMessageStatus.DeadLettered" />.
    ///     Default: <see cref="OutboxMessageEntity.DefaultMaxRetries" />.
    /// </summary>
    public int MaxRetries { get; set; } = OutboxMessageEntity.DefaultMaxRetries;

    /// <summary>
    ///     Tempo de retenção de mensagens processadas antes de serem removidas pela rotina de cleanup.
    ///     Default: 7 dias.
    /// </summary>
    public TimeSpan ProcessedRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    ///     Permite o fallback LINQ (sem FOR UPDATE SKIP LOCKED) em providers que não suportam
    ///     raw SQL, como InMemory (testes). <b>Não habilite em produção com múltiplas instâncias</b>:
    ///     o fallback não garante exclusão de lote e pode gerar double-delivery.
    ///     Env var: <c>OUTBOX__ALLOW_NON_LOCKING_FALLBACK</c>.
    /// </summary>
    public bool AllowNonLockingFallback { get; set; } = false;
}
