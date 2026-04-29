namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

public sealed record RejeitarOrdemDeServicoRequest(Guid OrdemDeServicoId) : IOrdemDeServicoRequest;
