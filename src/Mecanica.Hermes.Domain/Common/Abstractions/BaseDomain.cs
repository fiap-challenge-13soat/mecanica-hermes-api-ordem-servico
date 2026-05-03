using Mecanica.Hermes.Domain.Common.Clock;

namespace Mecanica.Hermes.Domain.Common.Abstractions;

public abstract class BaseDomain
{
    /// <summary>
    ///     Construtor padrão usado pelo EF Core e por entidades cuja Id será atribuída pela
    ///     camada de persistência (geração no Insert) ou via <see cref="RestaurarBase" />.
    /// </summary>
    protected BaseDomain()
    {
    }

    /// <summary>
    ///     Construtor explícito para casos onde o Id é fornecido pela camada chamadora
    ///     (ex.: idempotência por Id externo na criação da Ordem de Serviço).
    /// </summary>
    protected BaseDomain(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; protected set; } = DomainClock.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    protected internal void MarkAsUpdated()
    {
        UpdatedAt = DomainClock.UtcNow;
    }

    protected internal void RestaurarBase(Guid id, DateTime createdAt, DateTime? updatedAt)
    {
        Id = id;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
