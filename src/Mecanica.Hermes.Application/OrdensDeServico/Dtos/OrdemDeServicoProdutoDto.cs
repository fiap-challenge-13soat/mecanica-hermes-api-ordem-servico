namespace Mecanica.Hermes.Application.OrdensDeServico.Dtos;

public sealed record OrdemDeServicoProdutoDto(
    Guid Id,
    Guid ProdutoId,
    decimal Valor,
    int Quantidade);
