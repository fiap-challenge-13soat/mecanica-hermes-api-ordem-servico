using System.Diagnostics.Metrics;

namespace Mecanica.Hermes.Infrastructure.Persistence.Outbox;

/// <summary>
///     Métricas de saúde do Outbox emitidas pelo <see cref="OutboxProcessor" />.
///     <list type="bullet">
///         <item><b>outbox.messages.dispatched</b> — contador de mensagens despachadas com sucesso.</item>
///         <item><b>outbox.messages.failed</b> — contador de falhas transientes (ainda em retry).</item>
///         <item><b>outbox.messages.dead_lettered</b> — contador de mensagens que esgotaram as tentativas.</item>
///     </list>
///     Todos os instrumentos usam o meter <c>MecanicaHermes.Outbox</c> para permitir filtragem
///     independente do meter de negócio.
/// </summary>
internal sealed class OutboxHealthMetrics : IDisposable
{
    public const string MeterName = "MecanicaHermes.Outbox";
    private readonly Counter<long> _deadLettered;
    private readonly Counter<long> _dispatched;
    private readonly Counter<long> _failed;

    private readonly Meter _meter;

    public OutboxHealthMetrics()
    {
        _meter = new Meter(MeterName);
        _dispatched = _meter.CreateCounter<long>(
            "outbox.messages.dispatched",
            description: "Mensagens do Outbox despachadas com sucesso.");
        _failed = _meter.CreateCounter<long>(
            "outbox.messages.failed",
            description: "Falhas transientes ao despachar mensagens do Outbox (ainda em retry).");
        _deadLettered = _meter.CreateCounter<long>(
            "outbox.messages.dead_lettered",
            description: "Mensagens do Outbox movidas para Dead Letter após esgotar tentativas.");
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public void MessageDispatched()
    {
        _dispatched.Add(1);
    }

    public void MessageFailed()
    {
        _failed.Add(1);
    }

    public void MessageDeadLettered()
    {
        _deadLettered.Add(1);
    }
}
