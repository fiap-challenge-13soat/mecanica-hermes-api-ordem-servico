namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;

public sealed record AddOrdemDeServicoProdutoRequest(Guid ProdutoId, decimal ValorUnitario, int Quantidade);