using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Infrastructure.Persistence.Events;
using MediatR;

namespace Mecanica.Hermes.Infrastructure.Observability.Handlers;

internal sealed class OrdemCriadaMetricsHandler(IOrdemDeServicoMetrics metrics)
    : INotificationHandler<DomainEventNotification<OrdemDeServicoCriadoEvent>>
{
    public Task Handle(DomainEventNotification<OrdemDeServicoCriadoEvent> notification,
        CancellationToken cancellationToken)
    {
        metrics.OrdemCriada();
        return Task.CompletedTask;
    }
}
