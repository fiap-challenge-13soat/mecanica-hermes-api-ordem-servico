namespace Mecanica.Hermes.Infrastructure.Persistence.Entities.Abstractions;

internal class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
}
