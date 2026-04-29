using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Infrastructure.Persistence.Events;
using MediatR;

namespace Mecanica.Hermes.Infrastructure.Observability.Handlers;

internal sealed class EtapaAvancadaMetricsHandler(IOrdemDeServicoMetrics metrics)
    : INotificationHandler<DomainEventNotification<OrdemDeServicoEtapaAvancadaEvent>>
{
    public Task Handle(DomainEventNotification<OrdemDeServicoEtapaAvancadaEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;
        metrics.EtapaAvancada(e.StatusAnterior.ToString(), e.StatusAtual.ToString());
        return Task.CompletedTask;
    }
}
