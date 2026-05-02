using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Infrastructure.Observability.Handlers;
using Mecanica.Hermes.Infrastructure.Persistence.Events;

namespace Mecanica.Hermes.Infrastructure.Tests.Observability;

/// <summary>
/// Testes dos handlers de métricas: validam que cada Domain Event dispara a métrica correta.
/// </summary>
[Trait("Category", "Unit")]
public class MetricsHandlersTests
{
    private readonly Mock<IOrdemDeServicoMetrics> _metrics = new();

    [Fact]
    public async Task OrdemCriadaHandler_DeveChamarOrdemCriada()
    {
        var handler = new OrdemCriadaMetricsHandler(_metrics.Object);
        var notification = new DomainEventNotification<OrdemDeServicoCriadoEvent>(
            new OrdemDeServicoCriadoEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x", OrdemDeServicoStatus.Recebida));

        await handler.Handle(notification, CancellationToken.None);

        _metrics.Verify(m => m.OrdemCriada(), Times.Once);
    }

    [Fact]
    public async Task EtapaAvancadaHandler_DeveChamarEtapaAvancadaComStatus()
    {
        var handler = new EtapaAvancadaMetricsHandler(_metrics.Object);
        var notification = new DomainEventNotification<OrdemDeServicoEtapaAvancadaEvent>(
            new OrdemDeServicoEtapaAvancadaEvent(
                Guid.NewGuid(), OrdemDeServicoStatus.Recebida, OrdemDeServicoStatus.EmDiagnostico, 5.0));

        await handler.Handle(notification, CancellationToken.None);

        _metrics.Verify(m => m.EtapaAvancada("Recebida", "EmDiagnostico"), Times.Once);
    }

    [Fact]
    public async Task EtapaRevertidaHandler_DeveChamarEtapaRevertidaComStatus()
    {
        var handler = new EtapaRevertidaMetricsHandler(_metrics.Object);
        var notification = new DomainEventNotification<OrdemDeServicoEtapaRevertidaEvent>(
            new OrdemDeServicoEtapaRevertidaEvent(
                Guid.NewGuid(), OrdemDeServicoStatus.EmDiagnostico, OrdemDeServicoStatus.Recebida, 2.0));

        await handler.Handle(notification, CancellationToken.None);

        _metrics.Verify(m => m.EtapaRevertida("EmDiagnostico", "Recebida"), Times.Once);
    }

    [Fact]
    public async Task OrdemCanceladaHandler_DeveChamarOrdemCanceladaComStatusAnterior()
    {
        var handler = new OrdemCanceladaMetricsHandler(_metrics.Object);
        var notification = new DomainEventNotification<OrdemDeServicoCanceladaEvent>(
            new OrdemDeServicoCanceladaEvent(Guid.NewGuid(), OrdemDeServicoStatus.EmDiagnostico));

        await handler.Handle(notification, CancellationToken.None);

        _metrics.Verify(m => m.OrdemCancelada("EmDiagnostico"), Times.Once);
    }
}
