using MassTransit;

namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Integration;

[EntityName("pagamento.recusado")]
public sealed record PagamentoRecusadoEvent(
    Guid OrdemDeServicoId,
    Guid PagamentoId,
    string Motivo,
    DateTime RecusadoEm);
