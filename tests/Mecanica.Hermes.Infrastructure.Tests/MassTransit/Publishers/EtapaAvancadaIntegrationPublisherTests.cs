using MassTransit;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Contracts.Integration;
using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Infrastructure.MassTransit.Publishers;
using Mecanica.Hermes.Infrastructure.Persistence.Events;
using Mecanica.Hermes.TestUtils.Builders;

namespace Mecanica.Hermes.Infrastructure.Tests.MassTransit.Publishers;

/// <summary>
///     Testes unitários do <see cref="EtapaAvancadaIntegrationPublisher" />:
///     traduz <see cref="OrdemDeServicoEtapaAvancadaEvent" /> de domínio em eventos de
///     integração cross-service (Cadastros / Pagamentos), conforme o status alvo.
/// </summary>
[Trait("Category", "Unit")]
public class EtapaAvancadaIntegrationPublisherTests
{
    private static DomainEventNotification<OrdemDeServicoEtapaAvancadaEvent> Notif(
        Guid ordemId,
        OrdemDeServicoStatus statusAtual,
        OrdemDeServicoStatus statusAnterior = OrdemDeServicoStatus.Recebida)
    {
        return new DomainEventNotification<OrdemDeServicoEtapaAvancadaEvent>(
            new OrdemDeServicoEtapaAvancadaEvent(ordemId, statusAnterior, statusAtual));
    }

    [Fact]
    public async Task Handle_QuandoStatusAguardandoAprovacao_PublicaAguardandoAprovacaoEvent()
    {
        var ordemId = Guid.NewGuid();
        var endpoint = new Mock<IPublishEndpoint>();
        endpoint.Setup(e => e.Publish(
                It.IsAny<OrdemDeServicoAguardandoAprovacaoEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var reader = new Mock<IOrdemDeServicoReader>(MockBehavior.Strict);

        var sut = new EtapaAvancadaIntegrationPublisher(endpoint.Object, reader.Object);

        await sut.Handle(Notif(ordemId, OrdemDeServicoStatus.AguardandoAprovacao), CancellationToken.None);

        endpoint.Verify(
            e => e.Publish(
                It.Is<OrdemDeServicoAguardandoAprovacaoEvent>(ev => ev.OrdemDeServicoId == ordemId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        endpoint.VerifyNoOtherCalls();
        reader.VerifyNoOtherCalls(); // não consulta a OS para este caminho
    }

    [Fact]
    public async Task Handle_QuandoStatusAguardandoPagamento_PublicaAguardandoPagamentoComClienteEValor()
    {
        var ordemId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();

        var ordem = new OrdemDeServicoBuilder()
            .ComId(ordemId)
            .ComCliente(clienteId)
            .ComProduto(valor: 100m, quantidade: 2) // ValorTotal = 200
            .ComServico(valor: 50m) // + 50
            .Build();

        var endpoint = new Mock<IPublishEndpoint>();
        endpoint.Setup(e => e.Publish(
                It.IsAny<OrdemDeServicoAguardandoPagamentoEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var reader = new Mock<IOrdemDeServicoReader>();
        reader.Setup(r => r.GetByIdAsync(ordemId)).ReturnsAsync(ordem);

        var sut = new EtapaAvancadaIntegrationPublisher(endpoint.Object, reader.Object);

        await sut.Handle(Notif(ordemId, OrdemDeServicoStatus.AguardandoPagamento), CancellationToken.None);

        endpoint.Verify(
            e => e.Publish(
                It.Is<OrdemDeServicoAguardandoPagamentoEvent>(ev =>
                    ev.OrdemDeServicoId == ordemId &&
                    ev.ClienteId == clienteId &&
                    ev.Valor == 250m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoStatusAguardandoPagamentoEOSNaoEncontrada_LancaInvalidOperationException()
    {
        var ordemId = Guid.NewGuid();

        var endpoint = new Mock<IPublishEndpoint>(MockBehavior.Strict);
        var reader = new Mock<IOrdemDeServicoReader>();
        reader.Setup(r => r.GetByIdAsync(ordemId)).ReturnsAsync((OrdemDeServico?)null);

        var sut = new EtapaAvancadaIntegrationPublisher(endpoint.Object, reader.Object);

        var act = async () => await sut.Handle(
            Notif(ordemId, OrdemDeServicoStatus.AguardandoPagamento),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(ex => ex.Message.Contains(ordemId.ToString()));

        endpoint.VerifyNoOtherCalls(); // strict — nada deve ser publicado
    }

    [Theory]
    [InlineData(OrdemDeServicoStatus.Recebida)]
    [InlineData(OrdemDeServicoStatus.EmDiagnostico)]
    [InlineData(OrdemDeServicoStatus.EmExecucao)]
    [InlineData(OrdemDeServicoStatus.ManutencaoFinalizada)]
    [InlineData(OrdemDeServicoStatus.PagamentoConfirmado)]
    [InlineData(OrdemDeServicoStatus.Entregue)]
    [InlineData(OrdemDeServicoStatus.Rejeitada)]
    [InlineData(OrdemDeServicoStatus.Cancelada)]
    public async Task Handle_QuandoStatusNaoEhAguardando_NaoPublicaNadaENaoConsultaReader(
        OrdemDeServicoStatus statusAtual)
    {
        var endpoint = new Mock<IPublishEndpoint>(MockBehavior.Strict);
        var reader = new Mock<IOrdemDeServicoReader>(MockBehavior.Strict);

        var sut = new EtapaAvancadaIntegrationPublisher(endpoint.Object, reader.Object);

        await sut.Handle(Notif(Guid.NewGuid(), statusAtual), CancellationToken.None);

        endpoint.VerifyNoOtherCalls();
        reader.VerifyNoOtherCalls();
    }
}
