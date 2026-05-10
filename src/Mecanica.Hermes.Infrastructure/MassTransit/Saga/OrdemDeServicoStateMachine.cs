using MassTransit;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Events;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Infrastructure.MassTransit.Saga;

/// <summary>
///     SAGA que serializa as operações sobre uma <c>OrdemDeServico</c>, garantindo que apenas
///     uma operação por <c>OrdemDeServicoId</c> seja processada por vez. Mensagens recebidas
///     enquanto o estado é <see cref="Processing" /> são descartadas (Ignore), evitando o
///     processamento de mensagens duplicadas devido a requests concorrentes.
///     <para>
///         <b>Timeout sentinel:</b> ao entrar em <c>Processing</c>, um <c>OperationTimeout</c>
///         agendado via <c>DelayedMessageScheduler</c> é disparado após
///         <see cref="OperationTimeoutDelay" />. Caso a operação não finalize, a SAGA retorna para
///         <c>Initial</c> e regista o timeout como <c>UltimoMotivoFalha</c>.
///     </para>
/// </summary>
public class OrdemDeServicoStateMachine : MassTransitStateMachine<OrdemDeServicoState>
{
    /// <summary>
    ///     Prazo máximo de uma operação antes de ser abortada por timeout.
    ///     Default: 5 minutos. Override via env var <c>SAGA__OPERATION_TIMEOUT</c>
    ///     (formato <see cref="TimeSpan" />, ex.: <c>"00:00:30"</c>) — útil em E2E para
    ///     exercitar o pathway de timeout sem aguardar 5min reais.
    /// </summary>
    public static readonly TimeSpan OperationTimeoutDelay = ResolveOperationTimeout();

    private static TimeSpan ResolveOperationTimeout()
    {
        var raw = Environment.GetEnvironmentVariable("SAGA__OPERATION_TIMEOUT");
        if (!string.IsNullOrWhiteSpace(raw) && TimeSpan.TryParse(raw, out var parsed) && parsed > TimeSpan.Zero)
            return parsed;
        return TimeSpan.FromMinutes(5);
    }

    public OrdemDeServicoStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Schedule(() => OperationTimeout,
            state => state.OperationTimeoutTokenId,
            s =>
            {
                s.Delay = OperationTimeoutDelay;
                s.Received = e => e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId);
            });

        ConfigureCorrelations();

        Initially(
            When(CreateOrdemDeServicoRequested)
                .StartProcessing(nameof(CreateOrdemDeServicoRequested))
                .Schedule(OperationTimeout,
                    ctx => new OrdemDeServicoOperationTimedOut(ctx.Saga.CorrelationId,
                        nameof(CreateOrdemDeServicoRequested)))
                .Publish(ctx => new ExecuteCreateOrdemDeServico(
                    ctx.Saga.CorrelationId,
                    ctx.Message.ClienteId,
                    ctx.Message.VeiculoId,
                    ctx.Message.ProblemaRelatado,
                    ctx.Message.Observacoes))
                .TransitionTo(Processing),
            When(AvancarEtapaRequested)
                .StartProcessing(nameof(AvancarEtapaRequested))
                .Schedule(OperationTimeout,
                    ctx => new OrdemDeServicoOperationTimedOut(ctx.Saga.CorrelationId, nameof(AvancarEtapaRequested)))
                .Publish(ctx => new ExecuteAvancarEtapa(ctx.Saga.CorrelationId))
                .TransitionTo(Processing),
            When(ReverterEtapaRequested)
                .StartProcessing(nameof(ReverterEtapaRequested))
                .Schedule(OperationTimeout,
                    ctx => new OrdemDeServicoOperationTimedOut(ctx.Saga.CorrelationId, nameof(ReverterEtapaRequested)))
                .Publish(ctx => new ExecuteReverterEtapa(ctx.Saga.CorrelationId))
                .TransitionTo(Processing),
            When(AprovarOrdemDeServicoRequested)
                .StartProcessing(nameof(AprovarOrdemDeServicoRequested))
                .Schedule(OperationTimeout,
                    ctx => new OrdemDeServicoOperationTimedOut(ctx.Saga.CorrelationId,
                        nameof(AprovarOrdemDeServicoRequested)))
                .Publish(ctx => new ExecuteAprovarOrdemDeServico(ctx.Saga.CorrelationId))
                .TransitionTo(Processing),
            When(RejeitarOrdemDeServicoRequested)
                .StartProcessing(nameof(RejeitarOrdemDeServicoRequested))
                .Schedule(OperationTimeout,
                    ctx => new OrdemDeServicoOperationTimedOut(ctx.Saga.CorrelationId,
                        nameof(RejeitarOrdemDeServicoRequested)))
                .Publish(ctx => new ExecuteRejeitarOrdemDeServico(ctx.Saga.CorrelationId))
                .TransitionTo(Processing),
            When(PagamentoAprovadoRequested)
                .StartProcessing(nameof(PagamentoAprovadoRequested))
                .Schedule(OperationTimeout,
                    ctx => new OrdemDeServicoOperationTimedOut(ctx.Saga.CorrelationId,
                        nameof(PagamentoAprovadoRequested)))
                .Publish(ctx => new ExecutePagamentoAprovado(ctx.Saga.CorrelationId))
                .TransitionTo(Processing),
            When(CancelarOrdemDeServicoRequested)
                .StartProcessing(nameof(CancelarOrdemDeServicoRequested))
                .Schedule(OperationTimeout,
                    ctx => new OrdemDeServicoOperationTimedOut(ctx.Saga.CorrelationId,
                        nameof(CancelarOrdemDeServicoRequested)))
                .Publish(ctx => new ExecuteCancelarOrdemDeServico(ctx.Saga.CorrelationId))
                .TransitionTo(Processing),
            When(AddProdutoRequested)
                .StartProcessing(nameof(AddProdutoRequested))
                .Schedule(OperationTimeout,
                    ctx => new OrdemDeServicoOperationTimedOut(ctx.Saga.CorrelationId, nameof(AddProdutoRequested)))
                .Publish(ctx => new ExecuteAddProduto(
                    ctx.Saga.CorrelationId,
                    ctx.Message.ProdutoId,
                    ctx.Message.ValorVenda,
                    ctx.Message.Quantidade))
                .TransitionTo(Processing),
            When(AddServicoRequested)
                .StartProcessing(nameof(AddServicoRequested))
                .Schedule(OperationTimeout,
                    ctx => new OrdemDeServicoOperationTimedOut(ctx.Saga.CorrelationId, nameof(AddServicoRequested)))
                .Publish(ctx => new ExecuteAddServico(
                    ctx.Saga.CorrelationId,
                    ctx.Message.ServicoId,
                    ctx.Message.ValorVenda))
                .TransitionTo(Processing),
            When(RemoveProdutoRequested)
                .StartProcessing(nameof(RemoveProdutoRequested))
                .Schedule(OperationTimeout,
                    ctx => new OrdemDeServicoOperationTimedOut(ctx.Saga.CorrelationId, nameof(RemoveProdutoRequested)))
                .Publish(ctx => new ExecuteRemoveProduto(ctx.Saga.CorrelationId, ctx.Message.ProdutoId))
                .TransitionTo(Processing),
            When(RemoveServicoRequested)
                .StartProcessing(nameof(RemoveServicoRequested))
                .Schedule(OperationTimeout,
                    ctx => new OrdemDeServicoOperationTimedOut(ctx.Saga.CorrelationId, nameof(RemoveServicoRequested)))
                .Publish(ctx => new ExecuteRemoveServico(ctx.Saga.CorrelationId, ctx.Message.ServicoId))
                .TransitionTo(Processing),
            // Eventos de finalização/timeout chegando no estado Initial são silenciosamente ignorados
            // (reentrega tardia após a SAGA já ter voltado ao estado idle).
            Ignore(OperationCompleted),
            Ignore(OperationFailed),
            Ignore(OperationTimeout.Received)
        );

        // Em Processing apenas aceitamos a finalização da operação corrente.
        // Toda nova request é descartada (Ignore), garantindo "no máximo uma operação em voo por OrdemDeServicoId".
        During(Processing,
            Ignore(CreateOrdemDeServicoRequested),
            Ignore(AvancarEtapaRequested),
            Ignore(ReverterEtapaRequested),
            Ignore(AprovarOrdemDeServicoRequested),
            Ignore(RejeitarOrdemDeServicoRequested),
            Ignore(PagamentoAprovadoRequested),
            Ignore(CancelarOrdemDeServicoRequested),
            Ignore(AddProdutoRequested),
            Ignore(AddServicoRequested),
            Ignore(RemoveProdutoRequested),
            Ignore(RemoveServicoRequested),
            When(OperationCompleted)
                .Unschedule(OperationTimeout)
                .Then(ctx =>
                {
                    ctx.Saga.OperacaoEmAndamento = null;
                    ctx.Saga.OperacaoIniciadaEm = null;
                    ctx.Saga.UltimoMotivoFalha = null;
                })
                .TransitionTo(Initial),
            When(OperationFailed)
                .Unschedule(OperationTimeout)
                .Then(ctx =>
                {
                    ctx.Saga.OperacaoEmAndamento = null;
                    ctx.Saga.OperacaoIniciadaEm = null;
                    ctx.Saga.UltimoMotivoFalha = ctx.Message.Motivo;
                })
                .TransitionTo(Initial),
            When(OperationTimeout.Received)
                .Then(ctx =>
                {
                    ctx.Saga.UltimoMotivoFalha =
                        $"Timeout após {OperationTimeoutDelay.TotalMinutes}min na operação '{ctx.Message.Operacao}'.";
                    ctx.Saga.OperacaoEmAndamento = null;
                    ctx.Saga.OperacaoIniciadaEm = null;
                })
                .TransitionTo(Initial)
        );
    }

    public State Processing { get; } = null!;

    public Event<CreateOrdemDeServicoRequest> CreateOrdemDeServicoRequested { get; } = null!;
    public Event<AvancarEtapaRequest> AvancarEtapaRequested { get; } = null!;
    public Event<ReverterEtapaRequest> ReverterEtapaRequested { get; } = null!;
    public Event<AprovarOrdemDeServicoRequest> AprovarOrdemDeServicoRequested { get; } = null!;
    public Event<RejeitarOrdemDeServicoRequest> RejeitarOrdemDeServicoRequested { get; } = null!;
    public Event<PagamentoAprovadoRequest> PagamentoAprovadoRequested { get; } = null!;
    public Event<CancelarOrdemDeServicoRequest> CancelarOrdemDeServicoRequested { get; } = null!;
    public Event<AddProdutoRequest> AddProdutoRequested { get; } = null!;
    public Event<AddServicoRequest> AddServicoRequested { get; } = null!;
    public Event<RemoveProdutoRequest> RemoveProdutoRequested { get; } = null!;
    public Event<RemoveServicoRequest> RemoveServicoRequested { get; } = null!;

    public Event<OrdemDeServicoOperationCompleted> OperationCompleted { get; } = null!;
    public Event<OrdemDeServicoOperationFailed> OperationFailed { get; } = null!;

    /// <summary>
    ///     Schedule de timeout: ao entrar em <c>Processing</c>, agenda
    ///     <see cref="OrdemDeServicoOperationTimedOut" /> para disparar após
    ///     <see cref="OperationTimeoutDelay" />. Cancelado quando a operação finaliza.
    /// </summary>
    public Schedule<OrdemDeServicoState, OrdemDeServicoOperationTimedOut> OperationTimeout { get; } = null!;

    private void ConfigureCorrelations()
    {
        Event(() => CreateOrdemDeServicoRequested, e => e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId));
        Event(() => AvancarEtapaRequested, e => e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId));
        Event(() => ReverterEtapaRequested, e => e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId));
        Event(() => AprovarOrdemDeServicoRequested, e => e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId));
        Event(() => RejeitarOrdemDeServicoRequested, e => e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId));
        Event(() => PagamentoAprovadoRequested, e => e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId));
        Event(() => CancelarOrdemDeServicoRequested, e => e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId));
        Event(() => AddProdutoRequested, e => e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId));
        Event(() => AddServicoRequested, e => e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId));
        Event(() => RemoveProdutoRequested, e => e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId));
        Event(() => RemoveServicoRequested, e => e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId));

        // Eventos de finalização nunca devem criar uma nova instância de SAGA.
        Event(() => OperationCompleted, e =>
        {
            e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId);
            e.OnMissingInstance(m => m.Discard());
        });
        Event(() => OperationFailed, e =>
        {
            e.CorrelateById(ctx => ctx.Message.OrdemDeServicoId);
            e.OnMissingInstance(m => m.Discard());
        });
    }
}

internal static class OrdemDeServicoStateMachineExtensions
{
    public static EventActivityBinder<OrdemDeServicoState, T> StartProcessing<T>(
        this EventActivityBinder<OrdemDeServicoState, T> binder,
        string operacao)
        where T : class
    {
        return binder.Then(ctx =>
        {
            ctx.Saga.OperacaoEmAndamento = operacao;
            ctx.Saga.OperacaoIniciadaEm = DateTime.UtcNow;
        });
    }
}
