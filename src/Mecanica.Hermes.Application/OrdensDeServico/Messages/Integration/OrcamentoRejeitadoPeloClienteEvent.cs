using MassTransit;

namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Integration;

[EntityName("orcamento.rejeitado-pelo-cliente")]
public sealed record OrcamentoRejeitadoPeloClienteEvent(
    Guid OrdemDeServicoId,
    DateTime RejeitadoEm,
    string? Motivo);
