using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mecanica.Hermes.Infrastructure.Persistence.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.OccurredOn).IsRequired();
        builder.Property(x => x.Status).IsRequired()
            .HasConversion<int>();
        builder.Property(x => x.ProcessedOn);
        builder.Property(x => x.RetryCount).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.Property(x => x.TraceContext).HasMaxLength(200);

        // Índice parcial (PostgreSQL-específico): o background processor faz scan apenas de
        // mensagens com Status = 0 (Pending). Mantém o custo baixo mesmo com tabela volumosa.
        // Nota: HasFilter com SQL literal é provider-specific — migrações devem validar em PG.
        builder.HasIndex(x => x.OccurredOn)
            .HasDatabaseName("IX_OutboxMessages_OccurredOn_Pending")
            .HasFilter("\"Status\" = 0");
    }
}
