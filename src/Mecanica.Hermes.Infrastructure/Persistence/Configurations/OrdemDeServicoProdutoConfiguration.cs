using Mecanica.Hermes.Infrastructure.Persistence.Configurations.Helpers;
using Mecanica.Hermes.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mecanica.Hermes.Infrastructure.Persistence.Configurations;

internal class OrdemDeServicoProdutoConfiguration : IEntityTypeConfiguration<OrdemDeServicoProdutoEntity>
{
    public void Configure(EntityTypeBuilder<OrdemDeServicoProdutoEntity> builder)
    {
        builder.ToTable("OrdemDeServicoProdutos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrdemDeServicoId)
            .IsRequired();

        builder.Property(x => x.ProdutoId)
            .IsRequired();

        builder.Property(x => x.Valor)
            .HasConversion(
                valor => valor.Valor,
                valorDecimal => ValorVendaVoHelper.RecriarValor(valorDecimal))
            .HasColumnName("Valor")
            .IsRequired();

        builder.Property(x => x.Quantidade)
            .HasConversion(
                quantidade => quantidade.Valor,
                valor => QuantidadeVoHelper.RecriarQuantidade(valor))
            .HasColumnName("Quantidade")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);
    }
}
