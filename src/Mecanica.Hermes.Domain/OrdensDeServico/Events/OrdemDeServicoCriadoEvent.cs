using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Events;

[DomainEventName("ordem-de-servico.criado.v1")]
public record OrdemDeServicoCriadoEvent(
    Guid OrdemDeServicoId,
    Guid ClienteId,
    Guid VeiculoId,
    string? ProblemaRelatado,
    OrdemDeServicoStatus StatusInicial) : IDomainEvent;
