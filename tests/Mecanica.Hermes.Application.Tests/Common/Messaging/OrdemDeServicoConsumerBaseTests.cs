using MassTransit.Testing;
using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Events;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Shared.Core.Persistence;
using Mecanica.Hermes.Shared.Core.Results;
using Mecanica.Hermes.TestUtils.Builders;

namespace Mecanica.Hermes.Application.Tests.Common.Messaging;

/// <summary>
///     Testa o pipeline genérico (Template Method) em <see cref="OrdemDeServicoConsumerBase{TCommand}" />.
///     Usa um consumer de teste (<see cref="TestConsumer" />) para exercitar todos os ramos da base
///     (load, preconditions, execute, publish) sem depender dos consumers concretos — estes têm
///     apenas 1 teste de "feliz" específico cada.
/// </summary>
[Trait("Category", "Unit")]
public class OrdemDeServicoConsumerBaseTests
{
    private readonly Mock<IOrdemDeServicoMetrics> _metrics = new();
    private readonly Mock<IOrdemDeServicoRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private async Task<(InMemoryTestHarness harness, IConsumerTestHarness<TestConsumer> consumer)> StartHarnessAsync(
        Func<OrdemDeServico, Result> execute,
        Func<OrdemDeServico, Result>? precondition = null,
        string? metricKey = null)
    {
        var harness = new InMemoryTestHarness();
        var consumer = harness.Consumer(() => new TestConsumer(
            _repository.Object, _uow.Object, _metrics.Object, execute, precondition, metricKey));

        await harness.Start();
        return (harness, consumer);
    }

    [Fact]
    public async Task Consume_FluxoFeliz_DeveAtualizarCommitarEPublicarCompleted()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);
        _repository.Setup(r => r.UpdateAsync(It.IsAny<OrdemDeServico>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var (harness, _) = await StartHarnessAsync(_ => Result.Ok());
        try
        {
            await harness.InputQueueSendEndpoint.Send(new TestExecuteCommand(ordem.Id));

            (await harness.Consumed.Any<TestExecuteCommand>()).Should().BeTrue();
            (await harness.Published.Any<OrdemDeServicoOperationCompleted>(x =>
                x.Context.Message.OrdemDeServicoId == ordem.Id)).Should().BeTrue();

            _repository.Verify(r => r.UpdateAsync(ordem), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_QuandoOrdemNaoExiste_DevePublicarFailedERegistrarMetricaNaoEncontrada()
    {
        var ordemId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(ordemId)).ReturnsAsync((OrdemDeServico?)null);

        var (harness, _) = await StartHarnessAsync(_ => Result.Ok(), metricKey: "test_op");
        try
        {
            await harness.InputQueueSendEndpoint.Send(new TestExecuteCommand(ordemId));

            (await harness.Published.Any<OrdemDeServicoOperationFailed>(x =>
                x.Context.Message.OrdemDeServicoId == ordemId &&
                x.Context.Message.Motivo.Contains("não encontrada"))).Should().BeTrue();

            _repository.Verify(r => r.UpdateAsync(It.IsAny<OrdemDeServico>()), Times.Never);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
            _metrics.Verify(m => m.ErroRegistrado("test_op", "nao_encontrada"), Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_QuandoPreconditionFalha_DevePublicarFailedERegistrarMetricaStatusInvalido()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var (harness, _) = await StartHarnessAsync(
            _ => Result.Ok(),
            _ => Result.BadRequest("status inválido"),
            "test_op");
        try
        {
            await harness.InputQueueSendEndpoint.Send(new TestExecuteCommand(ordem.Id));

            (await harness.Published.Any<OrdemDeServicoOperationFailed>(x =>
                x.Context.Message.Motivo.Contains("status inválido"))).Should().BeTrue();

            _repository.Verify(r => r.UpdateAsync(It.IsAny<OrdemDeServico>()), Times.Never);
            _metrics.Verify(m => m.ErroRegistrado("test_op", "status_invalido"), Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_QuandoExecuteFalha_DevePublicarFailedERegistrarMetricaStatusInvalido()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var (harness, _) = await StartHarnessAsync(
            _ => Result.BadRequest("regra quebrada"),
            metricKey: "test_op");
        try
        {
            await harness.InputQueueSendEndpoint.Send(new TestExecuteCommand(ordem.Id));

            (await harness.Published.Any<OrdemDeServicoOperationFailed>(x =>
                x.Context.Message.Motivo.Contains("regra quebrada"))).Should().BeTrue();

            _repository.Verify(r => r.UpdateAsync(It.IsAny<OrdemDeServico>()), Times.Never);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
            _metrics.Verify(m => m.ErroRegistrado("test_op", "status_invalido"), Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }

    // Command de teste (apenas para fins do harness)
    private sealed record TestExecuteCommand(Guid OrdemDeServicoId) : IOrdemDeServicoExecuteCommand;

    // Consumer de teste com comportamento configurável por caso
    private sealed class TestConsumer(
        IOrdemDeServicoRepository repo,
        IUnitOfWork uow,
        IOrdemDeServicoMetrics metrics,
        Func<OrdemDeServico, Result> execute,
        Func<OrdemDeServico, Result>? precondition = null,
        string? metricKey = null)
        : OrdemDeServicoMutateConsumerBase<TestExecuteCommand>(repo, uow, metrics)
    {
        protected override string OperacaoMetricKey => metricKey ?? base.OperacaoMetricKey;

        protected override Result ValidatePreconditions(OrdemDeServico ordem, TestExecuteCommand msg)
        {
            return precondition is null ? Result.Ok() : precondition(ordem);
        }

        protected override Result Execute(OrdemDeServico ordem, TestExecuteCommand msg)
        {
            return execute(ordem);
        }
    }
}
