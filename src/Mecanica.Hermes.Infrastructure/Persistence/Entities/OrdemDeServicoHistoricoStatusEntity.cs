using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Infrastructure.Persistence.Entities.Abstractions;

namespace Mecanica.Hermes.Infrastructure.Persistence.Entities;

internal class OrdemDeServicoHistoricoStatusEntity : BaseEntity
{
    private OrdemDeServicoHistoricoStatusEntity()
    {
    }

    internal OrdemDeServicoHistoricoStatusEntity(
        Guid id,
        Guid ordemDeServicoId,
        OrdemDeServicoStatus statusAnterior,
        OrdemDeServicoStatus statusAtual,
        OrdemDeServicoStatus statusDestino,
        DateTime dataInicio,
        DateTime? dataFinalizacao,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        Id = id;
        OrdemDeServicoId = ordemDeServicoId;
        StatusAnterior = statusAnterior;
        StatusAtual = statusAtual;
        StatusDestino = statusDestino;
        DataInicio = dataInicio;
        DataFinalizacao = dataFinalizacao;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid OrdemDeServicoId { get; private set; }
    public OrdemDeServicoStatus StatusAnterior { get; private set; }
    public OrdemDeServicoStatus StatusAtual { get; private set; }
    public OrdemDeServicoStatus StatusDestino { get; private set; }
    public DateTime DataInicio { get; private set; }
    public DateTime? DataFinalizacao { get; private set; }

    // Referencias EF
    public virtual OrdemDeServicoEntity OrdemDeServico { get; private set; } = null!;
}
