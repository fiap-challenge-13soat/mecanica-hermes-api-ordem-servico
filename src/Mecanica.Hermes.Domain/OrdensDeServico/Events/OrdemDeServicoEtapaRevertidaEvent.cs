using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Shared.Core.Domain;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Events;

[DomainEventName("ordem-de-servico.etapa-revertida.v1")]
public record OrdemDeServicoEtapaRevertidaEvent(
    Guid OrdemDeServicoId,
    OrdemDeServicoStatus StatusAnterior,
    OrdemDeServicoStatus StatusAtual,
    double? DuracaoMinutes = null) : IDomainEvent;
