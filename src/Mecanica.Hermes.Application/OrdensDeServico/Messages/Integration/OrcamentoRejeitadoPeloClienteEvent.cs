using MassTransit;

namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Integration;

[EntityName("orcamento.rejeitado-pelo-cliente.v1")]
public sealed record OrcamentoRejeitadoPeloClienteEvent(
    Guid OrdemDeServicoId,
    DateTime RejeitadoEm,
    string? Motivo);
