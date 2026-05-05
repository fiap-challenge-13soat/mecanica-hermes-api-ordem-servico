using MassTransit;

namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Integration;

[EntityName("orcamento.aprovado-pelo-cliente")]
public sealed record OrcamentoAprovadoPeloClienteEvent(Guid OrdemDeServicoId, DateTime AprovadoEm);
