namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

public sealed record RemoveServicoRequest(
    Guid OrdemDeServicoId,
    Guid ServicoId) : IOrdemDeServicoRequest;
