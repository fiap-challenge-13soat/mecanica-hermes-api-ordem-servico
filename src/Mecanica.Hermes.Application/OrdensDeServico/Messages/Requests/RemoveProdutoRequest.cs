namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

public sealed record RemoveProdutoRequest(
    Guid OrdemDeServicoId,
    Guid ProdutoId) : IOrdemDeServicoRequest;
