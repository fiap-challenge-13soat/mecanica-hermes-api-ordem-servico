using MassTransit;
using MediatR;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Integration;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Infrastructure.Persistence.Events;

namespace Mecanica.Hermes.Infrastructure.MassTransit.Publishers;

internal sealed class EtapaAvancadaIntegrationPublisher(
    IPublishEndpoint publishEndpoint,
    IOrdemDeServicoRepository repository)
    : INotificationHandler<DomainEventNotification<OrdemDeServicoEtapaAvancadaEvent>>
{
    public async Task Handle(
        DomainEventNotification<OrdemDeServicoEtapaAvancadaEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.Event;

        if (e.StatusAtual == OrdemDeServicoStatus.AguardandoAprovacao)
        {
            await publishEndpoint.Publish(
                new OrdemDeServicoAguardandoAprovacaoEvent(e.OrdemDeServicoId),
                cancellationToken);
            return;
        }

        if (e.StatusAtual == OrdemDeServicoStatus.AguardandoPagamento)
        {
            var ordem = await repository.GetByIdAsync(e.OrdemDeServicoId);
            if (ordem is null) return;

            await publishEndpoint.Publish(
                new OrdemDeServicoAguardandoPagamentoEvent(
                    e.OrdemDeServicoId,
                    ordem.ClienteId,
                    ordem.ValorTotal),
                cancellationToken);
        }
    }
}
