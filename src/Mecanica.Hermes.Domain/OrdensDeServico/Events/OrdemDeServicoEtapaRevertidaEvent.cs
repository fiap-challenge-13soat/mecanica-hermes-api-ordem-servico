using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Events;

[DomainEventName("ordem-de-servico.etapa-revertida.v1")]
public record OrdemDeServicoEtapaRevertidaEvent(
    Guid OrdemDeServicoId,
    OrdemDeServicoStatus StatusAnterior,
    OrdemDeServicoStatus StatusAtual,
    double? DuracaoMinutes = null) : IDomainEvent;