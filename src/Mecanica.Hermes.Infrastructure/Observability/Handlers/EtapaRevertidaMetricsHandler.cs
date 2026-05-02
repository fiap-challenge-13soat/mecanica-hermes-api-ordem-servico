using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Infrastructure.Persistence.Events;
using MediatR;

namespace Mecanica.Hermes.Infrastructure.Observability.Handlers;

internal sealed class EtapaRevertidaMetricsHandler(IOrdemDeServicoMetrics metrics)
    : INotificationHandler<DomainEventNotification<OrdemDeServicoEtapaRevertidaEvent>>
{
    public Task Handle(DomainEventNotification<OrdemDeServicoEtapaRevertidaEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.Event;
        metrics.EtapaRevertida(e.StatusAnterior.ToString(), e.StatusAtual.ToString());
        return Task.CompletedTask;
    }
}
