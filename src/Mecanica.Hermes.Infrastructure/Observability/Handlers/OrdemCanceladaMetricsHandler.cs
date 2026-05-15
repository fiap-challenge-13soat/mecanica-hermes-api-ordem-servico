using System.Diagnostics.CodeAnalysis;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Infrastructure.Persistence.Events;
using MediatR;

namespace Mecanica.Hermes.Infrastructure.Observability.Handlers;

[ExcludeFromCodeCoverage(
    Justification = "Handler de métricas — instrumentação observada via telemetria, não por testes.")]
internal sealed class OrdemCanceladaMetricsHandler(IOrdemDeServicoMetrics metrics)
    : INotificationHandler<DomainEventNotification<OrdemDeServicoCanceladaEvent>>
{
    public Task Handle(DomainEventNotification<OrdemDeServicoCanceladaEvent> notification,
        CancellationToken cancellationToken)
    {
        metrics.OrdemCancelada(notification.Event.StatusAnterior.ToString());
        metrics.DistribuicaoAtualizada("Cancelada", notification.Event.StatusAnterior.ToString());
        return Task.CompletedTask;
    }
}
