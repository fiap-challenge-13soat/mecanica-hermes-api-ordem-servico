using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Domain.Common.Results;
using Mecanica.Hermes.Domain.OrdensDeServico.ValueObjects;

namespace Mecanica.Hermes.Domain.OrdensDeServico;

public class OrdemDeServicoProduto : BaseDomain
{
    private OrdemDeServicoProduto(Guid produtoId, ValorVendaVo valorVenda, QuantidadeVo quantidade)
    {
        ProdutoId = produtoId;
        ValorVenda = valorVenda;
        Quantidade = quantidade;
    }

    public Guid ProdutoId { get; private set; }
    public ValorVendaVo ValorVenda { get; }
    public QuantidadeVo Quantidade { get; }
    public decimal ValorTotal => ValorVenda.Valor * Quantidade.Valor;

    internal static Result<OrdemDeServicoProduto> Criar(Guid produtoId, decimal valorVenda, int quantidade)
    {
        var errors = new List<string>();

        var valorResult = ValorVendaVo.Criar(valorVenda);
        if (valorResult.IsFailure)
            errors.AddRange(valorResult.Errors);

        var quantidadeResult = QuantidadeVo.Criar(quantidade);
        if (quantidadeResult.IsFailure)
            errors.AddRange(quantidadeResult.Errors);

        if (errors.Count > 0)
            return Result<OrdemDeServicoProduto>.BadRequest(errors);

        var produto = new OrdemDeServicoProduto(produtoId, valorResult.Data!, quantidadeResult.Data!);

        return Result<OrdemDeServicoProduto>.Ok(produto);
    }

    internal static OrdemDeServicoProduto Restaurar(
        Guid id,
        Guid produtoId,
        ValorVendaVo valorVenda,
        QuantidadeVo quantidade,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        var item = new OrdemDeServicoProduto(produtoId, valorVenda, quantidade);
        item.RestaurarBase(id, createdAt, updatedAt);
        return item;
    }
}
