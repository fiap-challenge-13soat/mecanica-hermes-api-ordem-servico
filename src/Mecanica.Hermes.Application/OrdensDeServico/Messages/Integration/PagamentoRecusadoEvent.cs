using MassTransit;

namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Integration;

[EntityName("pagamento.recusado.v1")]
public sealed record PagamentoRecusadoEvent(
    Guid OrdemDeServicoId,
    Guid PagamentoId,
    string Motivo,
    DateTime RecusadoEm);
