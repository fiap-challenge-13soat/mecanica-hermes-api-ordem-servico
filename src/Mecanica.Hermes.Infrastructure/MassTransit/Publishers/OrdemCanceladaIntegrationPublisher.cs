using MassTransit;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Infrastructure.Persistence.Events;
using MediatR;
using IntegrationContract = Mecanica.Hermes.Contracts.Integration.OrdemDeServicoCanceladaEvent;

namespace Mecanica.Hermes.Infrastructure.MassTransit.Publishers;

/// <summary>
///     Traduz o domain event de cancelamento da OS no integration event correspondente
///     (<see cref="IntegrationContract" />), publicado no bus para que outros serviços
///     (em especial Pagamentos) reajam: cancelar pagamentos pendentes, parar polling MP,
///     etc.
/// </summary>
internal sealed class OrdemCanceladaIntegrationPublisher(IPublishEndpoint publishEndpoint)
    : INotificationHandler<DomainEventNotification<OrdemDeServicoCanceladaEvent>>
{
    public Task Handle(
        DomainEventNotification<OrdemDeServicoCanceladaEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.Event;
        return publishEndpoint.Publish(
            new IntegrationContract(
                e.OrdemDeServicoId,
                e.StatusAnterior.ToString(),
                DateTime.UtcNow),
            cancellationToken);
    }
}
