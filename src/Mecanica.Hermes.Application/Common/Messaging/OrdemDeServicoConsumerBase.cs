using MassTransit;
using Mecanica.Hermes.Application.Common.Exceptions;
using Mecanica.Hermes.Application.Common.Persistence;
using Mecanica.Hermes.Application.OrdensDeServico.Constants;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Events;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.Common.Results;
using Mecanica.Hermes.Domain.OrdensDeServico;

namespace Mecanica.Hermes.Application.Common.Messaging;

/// <summary>
/// Base abstrata que centraliza a publicação de eventos de saída
/// (<see cref="OrdemDeServicoOperationCompleted"/>/<see cref="OrdemDeServicoOperationFailed"/>),
/// o registro de métricas de erro e o nome canônico da operação. Compartilhada entre os
/// consumers que mutam (<see cref="OrdemDeServicoMutateConsumerBase{TCommand}"/>) e os que
/// criam (<see cref="OrdemDeServicoCreateConsumerBase{TCommand}"/>) o agregado.
/// </summary>
public abstract class OrdemDeServicoConsumerBase<TCommand> : IConsumer<TCommand>
    where TCommand : class, IOrdemDeServicoExecuteCommand
{
    protected readonly IOrdemDeServicoRepository Repository;
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly IOrdemDeServicoMetrics? Metrics;

    protected OrdemDeServicoConsumerBase(
        IOrdemDeServicoRepository repository,
        IUnitOfWork uow,
        IOrdemDeServicoMetrics? metrics = null)
    {
        Repository = repository;
        UnitOfWork = uow;
        Metrics = metrics;
    }

    /// <summary>
    /// Nome da operação publicado no evento <see cref="OrdemDeServicoOperationCompleted"/>/
    /// <see cref="OrdemDeServicoOperationFailed"/>. Faz parte do contrato com a SAGA.
    /// </summary>
    protected string OperacaoName { get; } = typeof(TCommand).Name;

    /// <summary>
    /// Chave de operação usada nas métricas. Override quando quiser uma chave em snake_case
    /// distinta do nome do tipo (ex.: <c>"avancar_etapa"</c> em vez de <c>"ExecuteAvancarEtapa"</c>).
    /// </summary>
    protected virtual string OperacaoMetricKey => OperacaoName;

    public abstract Task Consume(ConsumeContext<TCommand> context);

    protected Task PublishCompleted(ConsumeContext<TCommand> context) =>
        context.Publish(new OrdemDeServicoOperationCompleted(context.Message.OrdemDeServicoId, OperacaoName));

    protected Task PublishFailed(ConsumeContext<TCommand> context, IEnumerable<string> erros) =>
        context.Publish(new OrdemDeServicoOperationFailed(
            context.Message.OrdemDeServicoId,
            OperacaoName,
            string.Join("; ", erros)));

    protected Task PublishFailed(ConsumeContext<TCommand> context, string motivo) =>
        context.Publish(new OrdemDeServicoOperationFailed(
            context.Message.OrdemDeServicoId,
            OperacaoName,
            motivo));
}

/// <summary>
/// Pipeline (Template Method) para consumers que aplicam mutações a uma
/// <see cref="OrdemDeServico"/> existente.
///
/// Etapas:
/// <list type="number">
///   <item>Carrega o agregado por <c>OrdemDeServicoId</c>; se inexistente, registra métrica
///   <c>nao_encontrada</c> e publica <see cref="OrdemDeServicoOperationFailed"/>.</item>
///   <item>Valida pré-condições específicas via <see cref="ValidatePreconditions"/>.</item>
///   <item>Aplica a mutação delegando para <see cref="Execute"/>.</item>
///   <item>Persiste via <c>UpdateAsync</c> e comita o <c>UnitOfWork</c>.</item>
///   <item>Publica <see cref="OrdemDeServicoOperationCompleted"/>.</item>
/// </list>
/// </summary>
public abstract class OrdemDeServicoMutateConsumerBase<TCommand> : OrdemDeServicoConsumerBase<TCommand>
    where TCommand : class, IOrdemDeServicoExecuteCommand
{
    protected OrdemDeServicoMutateConsumerBase(
        IOrdemDeServicoRepository repository,
        IUnitOfWork uow,
        IOrdemDeServicoMetrics? metrics = null)
        : base(repository, uow, metrics)
    {
    }

    /// <summary>Aplica a regra de negócio ao agregado carregado.</summary>
    protected abstract Result Execute(OrdemDeServico ordem, TCommand msg);

    /// <summary>Hook opcional para validar pré-condições além do "agregado existe".</summary>
    protected virtual Result ValidatePreconditions(OrdemDeServico ordem, TCommand msg) => Result.Ok();

    public override async Task Consume(ConsumeContext<TCommand> context)
    {
        var msg = context.Message;

        var ordem = await Repository.GetByIdAsync(msg.OrdemDeServicoId);
        if (ordem is null)
        {
            Metrics?.ErroRegistrado(OperacaoMetricKey, OrdemDeServicoMetricKeys.ErrorTypes.NotFound);
            await PublishFailed(context, "Ordem de serviço não encontrada.");
            return;
        }

        var preconditions = ValidatePreconditions(ordem, msg);
        if (preconditions.IsFailure)
        {
            Metrics?.ErroRegistrado(OperacaoMetricKey, OrdemDeServicoMetricKeys.ErrorTypes.InvalidStatus);
            await PublishFailed(context, preconditions.Errors);
            return;
        }

        var result = Execute(ordem, msg);
        if (result.IsFailure)
        {
            Metrics?.ErroRegistrado(OperacaoMetricKey, OrdemDeServicoMetricKeys.ErrorTypes.InvalidStatus);
            await PublishFailed(context, result.Errors);
            return;
        }

        await Repository.UpdateAsync(ordem);

        try
        {
            await UnitOfWork.CommitAsync(context.CancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Aggregate was modified concurrently. MassTransit retry policy will redeliver
            // the message; the next attempt will reload the aggregate with fresh state.
            Metrics?.ErroRegistrado(OperacaoMetricKey, OrdemDeServicoMetricKeys.ErrorTypes.ConcurrencyConflict);
            await PublishFailed(context, "Conflito de concorrência. A operação será reprocessada.");
            return;
        }

        await PublishCompleted(context);
    }
}

/// <summary>
/// Pipeline (Template Method) para consumers que criam o agregado <see cref="OrdemDeServico"/>.
///
/// Etapas:
/// <list type="number">
///   <item>Constrói o agregado via <see cref="CreateAggregate"/>; em falha registra métrica
///   <c>validation_error</c> e publica <see cref="OrdemDeServicoOperationFailed"/>.</item>
///   <item>Persiste via <c>AddAsync</c> e comita o <c>UnitOfWork</c>.</item>
///   <item>Publica <see cref="OrdemDeServicoOperationCompleted"/>.</item>
/// </list>
/// </summary>
public abstract class OrdemDeServicoCreateConsumerBase<TCommand> : OrdemDeServicoConsumerBase<TCommand>
    where TCommand : class, IOrdemDeServicoExecuteCommand
{
    protected OrdemDeServicoCreateConsumerBase(
        IOrdemDeServicoRepository repository,
        IUnitOfWork uow,
        IOrdemDeServicoMetrics? metrics = null)
        : base(repository, uow, metrics)
    {
    }

    /// <summary>Constrói o agregado a partir do command. Cabe ao concreto chamar a factory de domínio.</summary>
    protected abstract Result<OrdemDeServico> CreateAggregate(TCommand msg);

    public override async Task Consume(ConsumeContext<TCommand> context)
    {
        var creation = CreateAggregate(context.Message);
        if (creation.IsFailure)
        {
            Metrics?.ErroRegistrado(OperacaoMetricKey, OrdemDeServicoMetricKeys.ErrorTypes.ValidationError);
            await PublishFailed(context, creation.Errors);
            return;
        }

        await Repository.AddAsync(creation.Data!);
        await UnitOfWork.CommitAsync(context.CancellationToken);

        await PublishCompleted(context);
    }
}
