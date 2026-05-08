using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Shared.Core.Domain;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Events;

[DomainEventName("ordem-de-servico.cancelada.v1")]
public record OrdemDeServicoCanceladaEvent(
    Guid OrdemDeServicoId,
    OrdemDeServicoStatus StatusAnterior) : IDomainEvent;
