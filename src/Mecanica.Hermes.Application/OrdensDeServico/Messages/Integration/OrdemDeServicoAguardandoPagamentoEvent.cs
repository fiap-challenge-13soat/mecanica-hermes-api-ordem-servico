using MassTransit;

namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Integration;

[EntityName("ordem-de-servico.aguardando-pagamento.v1")]
public sealed record OrdemDeServicoAguardandoPagamentoEvent(
    Guid OrdemDeServicoId,
    Guid ClienteId,
    decimal Valor);
