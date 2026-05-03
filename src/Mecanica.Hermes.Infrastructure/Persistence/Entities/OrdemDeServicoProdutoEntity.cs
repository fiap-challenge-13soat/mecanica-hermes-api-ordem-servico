using Mecanica.Hermes.Domain.OrdensDeServico.ValueObjects;
using Mecanica.Hermes.Infrastructure.Persistence.Entities.Abstractions;

namespace Mecanica.Hermes.Infrastructure.Persistence.Entities;

internal class OrdemDeServicoProdutoEntity : BaseEntity
{
    private OrdemDeServicoProdutoEntity()
    {
    }

    internal OrdemDeServicoProdutoEntity(
        Guid id,
        Guid ordemDeServicoId,
        Guid produtoId,
        ValorVendaVo valor,
        QuantidadeVo quantidade,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        Id = id;
        OrdemDeServicoId = ordemDeServicoId;
        ProdutoId = produtoId;
        Valor = valor;
        Quantidade = quantidade;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid OrdemDeServicoId { get; private set; }
    public Guid ProdutoId { get; private set; }
    public ValorVendaVo Valor { get; private set; } = null!;
    public QuantidadeVo Quantidade { get; private set; } = null!;

    // Referencias EF
    public virtual OrdemDeServicoEntity OrdemDeServico { get; private set; } = null!;
}
