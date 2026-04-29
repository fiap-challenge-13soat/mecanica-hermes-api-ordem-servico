namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

public sealed record CancelarOrdemDeServicoRequest(Guid OrdemDeServicoId) : IOrdemDeServicoRequest;
