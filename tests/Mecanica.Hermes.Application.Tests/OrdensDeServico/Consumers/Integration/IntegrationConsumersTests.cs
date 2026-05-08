using Mecanica.Hermes.Application.OrdensDeServico.Consumers.Integration;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Application.Tests.Common;
using Mecanica.Hermes.Contracts.Integration;
using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.TestUtils.Builders;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mecanica.Hermes.Application.Tests.OrdensDeServico.Consumers.Integration;

[Trait("Category", "Unit")]
public class IntegrationConsumersTests
{

    /// <summary>
    ///     Avança a OS via state machine para o estado-alvo. Lifecycle linear: Recebida →
    ///     EmDiagnostico → AguardandoAprovacao → EmExecucao → ManutencaoFinalizada →
    ///     AguardandoPagamento → PagamentoConfirmado → Entregue.
    /// </summary>
    private static OrdemDeServico OrdemEm(OrdemDeServicoStatus alvo)
    {
        return alvo switch
        {
            OrdemDeServicoStatus.Recebida =>
                new OrdemDeServicoBuilder().Build(),
            OrdemDeServicoStatus.EmDiagnostico =>
                new OrdemDeServicoBuilder().ComStatusAvancadoEm(1).Build(),
            OrdemDeServicoStatus.AguardandoAprovacao =>
                new OrdemDeServicoBuilder().ComStatusAvancadoEm(2).Build(),
            OrdemDeServicoStatus.EmExecucao =>
                new OrdemDeServicoBuilder().ComStatusAvancadoEm(3).Build(),
            OrdemDeServicoStatus.ManutencaoFinalizada =>
                new OrdemDeServicoBuilder().ComStatusAvancadoEm(4).Build(),
            OrdemDeServicoStatus.AguardandoPagamento =>
                new OrdemDeServicoBuilder().ComStatusAvancadoEm(5).Build(),
            OrdemDeServicoStatus.PagamentoConfirmado =>
                new OrdemDeServicoBuilder().ComStatusAvancadoEm(6).Build(),
            OrdemDeServicoStatus.Cancelada =>
                new OrdemDeServicoBuilder().Cancelada().Build(),
            _ => throw new ArgumentOutOfRangeException(nameof(alvo), alvo, "Estado não suportado pelo helper.")
        };
    }

    // ----------------------------------------------------------------------------------
    // OrcamentoAprovadoPeloCliente
    // ----------------------------------------------------------------------------------

    [Fact]
    public async Task OrcamentoAprovado_QuandoOSEstaAguardandoAprovacao_PublicaRequest()
    {
        var ordem = OrdemEm(OrdemDeServicoStatus.AguardandoAprovacao);
        var reader = new Mock<IOrdemDeServicoReader>();
        reader.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        AprovarOrdemDeServicoRequest? published = null;
        var ctx = FakeConsumeContext.MockFor(new OrcamentoAprovadoPeloClienteEvent(ordem.Id, DateTime.UtcNow));
        ctx.Setup(c => c.Publish(It.IsAny<AprovarOrdemDeServicoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AprovarOrdemDeServicoRequest, CancellationToken>((m, _) => published = m)
            .Returns(Task.CompletedTask);

        var sut = new OrcamentoAprovadoPeloClienteConsumer(
            reader.Object,
            new Mock<IOrdemDeServicoMetrics>().Object,
            NullLogger<OrcamentoAprovadoPeloClienteConsumer>.Instance);

        await sut.Consume(ctx.Object);

        published.Should().NotBeNull();
        published!.OrdemDeServicoId.Should().Be(ordem.Id);
    }

    [Fact]
    public async Task OrcamentoAprovado_QuandoOSJaEmExecucao_IgnoraEContaMetrica()
    {
        var ordem = OrdemEm(OrdemDeServicoStatus.EmExecucao);
        var reader = new Mock<IOrdemDeServicoReader>();
        reader.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var ctx = FakeConsumeContext.MockFor(new OrcamentoAprovadoPeloClienteEvent(ordem.Id, DateTime.UtcNow));
        var metrics = new Mock<IOrdemDeServicoMetrics>();

        var sut = new OrcamentoAprovadoPeloClienteConsumer(
            reader.Object, metrics.Object,
            NullLogger<OrcamentoAprovadoPeloClienteConsumer>.Instance);

        await sut.Consume(ctx.Object);

        ctx.Verify(c => c.Publish(It.IsAny<AprovarOrdemDeServicoRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        metrics.Verify(
            m => m.EventoIntegracaoIgnorado("orcamento.aprovado-pelo-cliente.v1", "EmExecucao"),
            Times.Once);
    }

    [Fact]
    public async Task OrcamentoAprovado_QuandoOSNaoExiste_IgnoraSilenciosamente()
    {
        var id = Guid.NewGuid();
        var reader = new Mock<IOrdemDeServicoReader>();
        reader.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((OrdemDeServico?)null);

        var ctx = FakeConsumeContext.MockFor(new OrcamentoAprovadoPeloClienteEvent(id, DateTime.UtcNow));
        var metrics = new Mock<IOrdemDeServicoMetrics>();

        var sut = new OrcamentoAprovadoPeloClienteConsumer(
            reader.Object, metrics.Object,
            NullLogger<OrcamentoAprovadoPeloClienteConsumer>.Instance);

        await sut.Consume(ctx.Object);

        ctx.Verify(c => c.Publish(It.IsAny<AprovarOrdemDeServicoRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ----------------------------------------------------------------------------------
    // OrcamentoRejeitadoPeloCliente
    // ----------------------------------------------------------------------------------

    [Fact]
    public async Task OrcamentoRejeitado_QuandoOSEstaAguardandoAprovacao_PublicaRequest()
    {
        var ordem = OrdemEm(OrdemDeServicoStatus.AguardandoAprovacao);
        var reader = new Mock<IOrdemDeServicoReader>();
        reader.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        RejeitarOrdemDeServicoRequest? published = null;
        var evento = new OrcamentoRejeitadoPeloClienteEvent(ordem.Id, DateTime.UtcNow, "Sem condição");
        var ctx = FakeConsumeContext.MockFor(evento);
        ctx.Setup(c => c.Publish(It.IsAny<RejeitarOrdemDeServicoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RejeitarOrdemDeServicoRequest, CancellationToken>((m, _) => published = m)
            .Returns(Task.CompletedTask);

        var sut = new OrcamentoRejeitadoPeloClienteConsumer(
            reader.Object,
            new Mock<IOrdemDeServicoMetrics>().Object,
            NullLogger<OrcamentoRejeitadoPeloClienteConsumer>.Instance);

        await sut.Consume(ctx.Object);

        published.Should().NotBeNull();
        published!.OrdemDeServicoId.Should().Be(ordem.Id);
    }

    [Fact]
    public async Task OrcamentoRejeitado_QuandoOSJaEmExecucao_IgnoraEContaMetrica()
    {
        var ordem = OrdemEm(OrdemDeServicoStatus.EmExecucao);
        var reader = new Mock<IOrdemDeServicoReader>();
        reader.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var evento = new OrcamentoRejeitadoPeloClienteEvent(ordem.Id, DateTime.UtcNow, "Sem condição");
        var ctx = FakeConsumeContext.MockFor(evento);
        var metrics = new Mock<IOrdemDeServicoMetrics>();

        var sut = new OrcamentoRejeitadoPeloClienteConsumer(
            reader.Object, metrics.Object,
            NullLogger<OrcamentoRejeitadoPeloClienteConsumer>.Instance);

        await sut.Consume(ctx.Object);

        ctx.Verify(c => c.Publish(It.IsAny<RejeitarOrdemDeServicoRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        metrics.Verify(
            m => m.EventoIntegracaoIgnorado("orcamento.rejeitado-pelo-cliente.v1", "EmExecucao"),
            Times.Once);
    }

    // ----------------------------------------------------------------------------------
    // PagamentoConfirmado
    // ----------------------------------------------------------------------------------

    [Fact]
    public async Task PagamentoConfirmado_QuandoOSEstaAguardandoPagamento_PublicaRequest()
    {
        var ordem = OrdemEm(OrdemDeServicoStatus.AguardandoPagamento);
        var reader = new Mock<IOrdemDeServicoReader>();
        reader.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        PagamentoAprovadoRequest? published = null;
        var ctx = FakeConsumeContext.MockFor(new PagamentoConfirmadoEvent(ordem.Id, Guid.NewGuid(), DateTime.UtcNow));
        ctx.Setup(c => c.Publish(It.IsAny<PagamentoAprovadoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PagamentoAprovadoRequest, CancellationToken>((m, _) => published = m)
            .Returns(Task.CompletedTask);

        var sut = new PagamentoConfirmadoConsumer(
            reader.Object,
            new Mock<IOrdemDeServicoMetrics>().Object,
            NullLogger<PagamentoConfirmadoConsumer>.Instance);

        await sut.Consume(ctx.Object);

        published.Should().NotBeNull();
        published!.OrdemDeServicoId.Should().Be(ordem.Id);
    }

    [Fact]
    public async Task PagamentoConfirmado_QuandoOSJaConfirmada_IgnoraEContaMetrica()
    {
        var ordem = OrdemEm(OrdemDeServicoStatus.PagamentoConfirmado);
        var reader = new Mock<IOrdemDeServicoReader>();
        reader.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var ctx = FakeConsumeContext.MockFor(new PagamentoConfirmadoEvent(ordem.Id, Guid.NewGuid(), DateTime.UtcNow));
        var metrics = new Mock<IOrdemDeServicoMetrics>();

        var sut = new PagamentoConfirmadoConsumer(
            reader.Object, metrics.Object,
            NullLogger<PagamentoConfirmadoConsumer>.Instance);

        await sut.Consume(ctx.Object);

        ctx.Verify(c => c.Publish(It.IsAny<PagamentoAprovadoRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        metrics.Verify(
            m => m.EventoIntegracaoIgnorado("pagamento.confirmado.v1", "PagamentoConfirmado"),
            Times.Once);
    }

    // ----------------------------------------------------------------------------------
    // PagamentoRecusado
    // ----------------------------------------------------------------------------------

    [Fact]
    public async Task PagamentoRecusado_QuandoOSEstaAguardandoPagamento_PublicaRequest()
    {
        var ordem = OrdemEm(OrdemDeServicoStatus.AguardandoPagamento);
        var reader = new Mock<IOrdemDeServicoReader>();
        reader.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        ReverterEtapaRequest? published = null;
        var evento = new PagamentoRecusadoEvent(ordem.Id, Guid.NewGuid(), "Sem saldo", DateTime.UtcNow);
        var ctx = FakeConsumeContext.MockFor(evento);
        ctx.Setup(c => c.Publish(It.IsAny<ReverterEtapaRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ReverterEtapaRequest, CancellationToken>((m, _) => published = m)
            .Returns(Task.CompletedTask);

        var sut = new PagamentoRecusadoConsumer(
            reader.Object,
            new Mock<IOrdemDeServicoMetrics>().Object,
            NullLogger<PagamentoRecusadoConsumer>.Instance);

        await sut.Consume(ctx.Object);

        published.Should().NotBeNull();
        published!.OrdemDeServicoId.Should().Be(ordem.Id);
    }

    [Fact]
    public async Task PagamentoRecusado_QuandoOSJaRevertida_IgnoraEContaMetrica()
    {
        var ordem = OrdemEm(OrdemDeServicoStatus.ManutencaoFinalizada);
        var reader = new Mock<IOrdemDeServicoReader>();
        reader.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var evento = new PagamentoRecusadoEvent(ordem.Id, Guid.NewGuid(), "Sem saldo", DateTime.UtcNow);
        var ctx = FakeConsumeContext.MockFor(evento);
        var metrics = new Mock<IOrdemDeServicoMetrics>();

        var sut = new PagamentoRecusadoConsumer(
            reader.Object, metrics.Object,
            NullLogger<PagamentoRecusadoConsumer>.Instance);

        await sut.Consume(ctx.Object);

        ctx.Verify(c => c.Publish(It.IsAny<ReverterEtapaRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        metrics.Verify(
            m => m.EventoIntegracaoIgnorado("pagamento.recusado.v1", "ManutencaoFinalizada"),
            Times.Once);
    }
}
