using Mecanica.Hermes.Domain.OrdensDeServico.ValueObjects;
using Mecanica.Hermes.Infrastructure.Persistence.Entities.Abstractions;

namespace Mecanica.Hermes.Infrastructure.Persistence.Entities;

internal class OrdemDeServicoServicoEntity : BaseEntity
{
    private OrdemDeServicoServicoEntity()
    {
    }

    internal OrdemDeServicoServicoEntity(
        Guid id,
        Guid ordemDeServicoId,
        Guid servicoId,
        ValorVendaVo valor,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        Id = id;
        OrdemDeServicoId = ordemDeServicoId;
        ServicoId = servicoId;
        Valor = valor;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid OrdemDeServicoId { get; private set; }
    public Guid ServicoId { get; private set; }
    public ValorVendaVo Valor { get; private set; } = null!;

    // Referencias EF
    public virtual OrdemDeServicoEntity OrdemDeServico { get; private set; } = null!;
}
