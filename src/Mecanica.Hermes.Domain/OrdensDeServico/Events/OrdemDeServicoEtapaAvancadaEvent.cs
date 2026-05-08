using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Shared.Core.Domain;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Events;

[DomainEventName("ordem-de-servico.etapa-avancada.v1")]
public record OrdemDeServicoEtapaAvancadaEvent(
    Guid OrdemDeServicoId,
    OrdemDeServicoStatus StatusAnterior,
    OrdemDeServicoStatus StatusAtual,
    double? DuracaoMinutes = null) : IDomainEvent;
