using Mecanica.Hermes.Infrastructure.Persistence.Configurations.Helpers;
using Mecanica.Hermes.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mecanica.Hermes.Infrastructure.Persistence.Configurations;

internal class OrdemDeServicoServicoConfiguration : IEntityTypeConfiguration<OrdemDeServicoServicoEntity>
{
    public void Configure(EntityTypeBuilder<OrdemDeServicoServicoEntity> builder)
    {
        builder.ToTable("OrdemDeServicoServicos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrdemDeServicoId)
            .IsRequired();

        builder.Property(x => x.ServicoId)
            .IsRequired();

        builder.Property(x => x.Valor)
            .HasConversion(
                valor => valor.Valor,
                valorDecimal => ValorVendaVoHelper.RecriarValor(valorDecimal))
            .HasColumnName("Valor")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);
    }
}
