using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Events;

[DomainEventName("ordem-de-servico.cancelada.v1")]
public record OrdemDeServicoCanceladaEvent(
    Guid OrdemDeServicoId,
    OrdemDeServicoStatus StatusAnterior) : IDomainEvent;
