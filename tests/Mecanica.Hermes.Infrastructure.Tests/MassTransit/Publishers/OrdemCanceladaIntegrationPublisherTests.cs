using MassTransit;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Infrastructure.MassTransit.Publishers;
using Mecanica.Hermes.Infrastructure.Persistence.Events;
using IntegrationContract = Mecanica.Hermes.Contracts.Integration.OrdemDeServicoCanceladaEvent;

namespace Mecanica.Hermes.Infrastructure.Tests.MassTransit.Publishers;

/// <summary>
///     Testes unitários do <see cref="OrdemCanceladaIntegrationPublisher" />:
///     traduz o domain event de cancelamento da OS no integration event correspondente,
///     publicado no bus para que outros serviços (em especial Pagamentos) reajam.
/// </summary>
[Trait("Category", "Unit")]
public class OrdemCanceladaIntegrationPublisherTests
{
    private static DomainEventNotification<OrdemDeServicoCanceladaEvent> Notif(
        Guid ordemId,
        OrdemDeServicoStatus statusAnterior)
    {
        return new DomainEventNotification<OrdemDeServicoCanceladaEvent>(
            new OrdemDeServicoCanceladaEvent(ordemId, statusAnterior));
    }

    [Fact]
    public async Task Handle_PublicaIntegrationEventComIdEStatusAnterior()
    {
        var ordemId = Guid.NewGuid();
        var endpoint = new Mock<IPublishEndpoint>();
        endpoint.Setup(e => e.Publish(
                It.IsAny<IntegrationContract>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new OrdemCanceladaIntegrationPublisher(endpoint.Object);

        var antes = DateTime.UtcNow;
        await sut.Handle(Notif(ordemId, OrdemDeServicoStatus.AguardandoPagamento), CancellationToken.None);
        var depois = DateTime.UtcNow;

        endpoint.Verify(
            e => e.Publish(
                It.Is<IntegrationContract>(ev =>
                    ev.OrdemDeServicoId == ordemId &&
                    ev.StatusAnterior == OrdemDeServicoStatus.AguardandoPagamento.ToString() &&
                    ev.OcorridoEm >= antes && ev.OcorridoEm <= depois),
                It.IsAny<CancellationToken>()),
            Times.Once);
        endpoint.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(OrdemDeServicoStatus.Recebida)]
    [InlineData(OrdemDeServicoStatus.AguardandoAprovacao)]
    [InlineData(OrdemDeServicoStatus.AguardandoPagamento)]
    [InlineData(OrdemDeServicoStatus.EmDiagnostico)]
    [InlineData(OrdemDeServicoStatus.EmExecucao)]
    public async Task Handle_PreservaStatusAnteriorComoString(OrdemDeServicoStatus statusAnterior)
    {
        var ordemId = Guid.NewGuid();
        var endpoint = new Mock<IPublishEndpoint>();
        endpoint.Setup(e => e.Publish(
                It.IsAny<IntegrationContract>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new OrdemCanceladaIntegrationPublisher(endpoint.Object);

        await sut.Handle(Notif(ordemId, statusAnterior), CancellationToken.None);

        endpoint.Verify(
            e => e.Publish(
                It.Is<IntegrationContract>(ev => ev.StatusAnterior == statusAnterior.ToString()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PropagaCancellationToken()
    {
        var endpoint = new Mock<IPublishEndpoint>();
        endpoint.Setup(e => e.Publish(
                It.IsAny<IntegrationContract>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new OrdemCanceladaIntegrationPublisher(endpoint.Object);

        using var cts = new CancellationTokenSource();
        await sut.Handle(Notif(Guid.NewGuid(), OrdemDeServicoStatus.Recebida), cts.Token);

        endpoint.Verify(
            e => e.Publish(It.IsAny<IntegrationContract>(), cts.Token),
            Times.Once);
    }
}
