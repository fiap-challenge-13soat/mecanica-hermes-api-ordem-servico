namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;

public sealed record ExecuteAddProduto(
    Guid OrdemDeServicoId,
    Guid ProdutoId,
    decimal ValorVenda,
    int Quantidade) : IOrdemDeServicoExecuteCommand;
