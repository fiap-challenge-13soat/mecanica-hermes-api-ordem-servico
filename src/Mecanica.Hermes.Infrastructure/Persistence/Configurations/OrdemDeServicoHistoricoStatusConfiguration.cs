using Mecanica.Hermes.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mecanica.Hermes.Infrastructure.Persistence.Configurations;

internal class
    OrdemDeServicoHistoricoStatusConfiguration : IEntityTypeConfiguration<OrdemDeServicoHistoricoStatusEntity>
{
    public void Configure(EntityTypeBuilder<OrdemDeServicoHistoricoStatusEntity> builder)
    {
        builder.ToTable("OrdemDeServicoHistoricoStatus");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrdemDeServicoId)
            .IsRequired();

        builder.Property(x => x.StatusAnterior)
            .HasConversion<string>()
            .HasColumnName("StatusAnterior")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.StatusAtual)
            .HasConversion<string>()
            .HasColumnName("StatusAtual")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.StatusDestino)
            .HasConversion<string>()
            .HasColumnName("StatusDestino")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DataInicio)
            .HasColumnName("DataInicio")
            .IsRequired();

        builder.Property(x => x.DataFinalizacao)
            .HasColumnName("DataFinalizacao");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);
    }
}
