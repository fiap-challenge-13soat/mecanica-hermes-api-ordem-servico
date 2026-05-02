using MassTransit;

namespace Mecanica.Hermes.Infrastructure.MassTransit.Saga;

public class OrdemDeServicoState : SagaStateMachineInstance, ISagaVersion
{
    /// <summary>
    ///     Estado corrente da SAGA (<c>Initial</c> ou <c>Processing</c>).
    /// </summary>
    public string CurrentState { get; set; } = null!;

    /// <summary>
    ///     Nome da operação atualmente em andamento (quando em <c>Processing</c>).
    ///     Útil para diagnósticos e logs de mensagens descartadas.
    /// </summary>
    public string? OperacaoEmAndamento { get; set; }

    /// <summary>
    ///     Momento em que a operação foi iniciada. Permite diagnóstico de operações travadas.
    /// </summary>
    public DateTime? OperacaoIniciadaEm { get; set; }

    /// <summary>
    ///     Última mensagem de falha registrada (quando aplicável).
    /// </summary>
    public string? UltimoMotivoFalha { get; set; }

    /// <summary>
    ///     Token do agendamento de timeout da operação corrente. Usado pelo
    ///     <c>Schedule</c> do MassTransit para cancelar o timer quando a operação
    ///     completa antes do prazo.
    /// </summary>
    public Guid? OperationTimeoutTokenId { get; set; }

    /// <summary>
    ///     Versão usada pelo MassTransit.MongoDb para controle de concorrência otimista (optimistic locking).
    ///     Incrementada automaticamente pelo repositório a cada atualização do documento.
    /// </summary>
    public int Version { get; set; }

    public Guid CorrelationId { get; set; }
}
