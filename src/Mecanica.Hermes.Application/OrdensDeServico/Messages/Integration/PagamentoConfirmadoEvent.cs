using MassTransit;

namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Integration;

[EntityName("pagamento.confirmado")]
public sealed record PagamentoConfirmadoEvent(
    Guid OrdemDeServicoId,
    Guid PagamentoId,
    DateTime ConfirmadoEm);
