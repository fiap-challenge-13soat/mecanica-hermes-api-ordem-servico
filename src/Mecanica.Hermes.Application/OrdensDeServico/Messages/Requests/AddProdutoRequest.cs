namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

public sealed record AddProdutoRequest(
    Guid OrdemDeServicoId,
    Guid ProdutoId,
    decimal ValorVenda,
    int Quantidade) : IOrdemDeServicoRequest;
