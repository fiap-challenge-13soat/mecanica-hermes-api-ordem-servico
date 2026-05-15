using System.Diagnostics.CodeAnalysis;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Infrastructure.Persistence.Events;
using MediatR;

namespace Mecanica.Hermes.Infrastructure.Observability.Handlers;

[ExcludeFromCodeCoverage(
    Justification = "Handler de métricas — instrumentação observada via telemetria, não por testes.")]
internal sealed class EtapaAvancadaMetricsHandler(IOrdemDeServicoMetrics metrics)
    : INotificationHandler<DomainEventNotification<OrdemDeServicoEtapaAvancadaEvent>>
{
    public Task Handle(DomainEventNotification<OrdemDeServicoEtapaAvancadaEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.Event;
        metrics.EtapaAvancada(e.StatusAnterior.ToString(), e.StatusAtual.ToString());
        metrics.DistribuicaoAtualizada(e.StatusAtual.ToString(), e.StatusAnterior.ToString());
        if (e.DuracaoMinutes.HasValue)
            metrics.DuracaoEtapaRegistrada(e.StatusAnterior.ToString(), e.DuracaoMinutes.Value);
        return Task.CompletedTask;
    }
}
