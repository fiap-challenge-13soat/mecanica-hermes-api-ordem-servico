using MassTransit;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Contracts.Integration;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Infrastructure.Persistence.Events;
using MediatR;

namespace Mecanica.Hermes.Infrastructure.MassTransit.Publishers;

internal sealed class EtapaAvancadaIntegrationPublisher(
    IPublishEndpoint publishEndpoint,
    IOrdemDeServicoReader reader)
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
            var ordem = await reader.GetByIdAsync(e.OrdemDeServicoId);
            if (ordem is null)
                throw new InvalidOperationException(
                    $"OS {e.OrdemDeServicoId} não encontrada ao publicar evento AguardandoPagamento. " +
                    "Inconsistência de dados: o domínio avançou de estado mas a OS não está persistida.");

            await publishEndpoint.Publish(
                new OrdemDeServicoAguardandoPagamentoEvent(
                    e.OrdemDeServicoId,
                    ordem.ClienteId,
                    ordem.ValorTotal),
                cancellationToken);
        }
    }
}
