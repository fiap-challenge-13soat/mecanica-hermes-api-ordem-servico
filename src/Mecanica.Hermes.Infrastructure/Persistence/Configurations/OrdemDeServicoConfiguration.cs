using Mecanica.Hermes.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mecanica.Hermes.Infrastructure.Persistence.Configurations;

internal class OrdemDeServicoConfiguration : IEntityTypeConfiguration<OrdemDeServicoEntity>
{
    public void Configure(EntityTypeBuilder<OrdemDeServicoEntity> builder)
    {
        builder.ToTable("OrdensDeServico");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClienteId)
            .IsRequired();

        builder.Property(x => x.VeiculoId)
            .IsRequired();

        builder.Property(x => x.ProblemaRelatado)
            .HasColumnName("ProblemaRelatado")
            .HasMaxLength(1000);

        builder.Property(x => x.Observacoes)
            .HasColumnName("Observacoes")
            .HasMaxLength(1000);

        builder.Property(x => x.StatusAtualId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.HasOne(x => x.StatusAtual)
            .WithMany()
            .HasForeignKey(x => x.StatusAtualId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.HistoricosStatus)
            .WithOne(x => x.OrdemDeServico)
            .HasForeignKey(x => x.OrdemDeServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Produtos)
            .WithOne(x => x.OrdemDeServico)
            .HasForeignKey(x => x.OrdemDeServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Servicos)
            .WithOne(x => x.OrdemDeServico)
            .HasForeignKey(x => x.OrdemDeServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Concorrência otimista via coluna de sistema xmin do PostgreSQL.
        // xmin é uma coluna MVCC nativa do Postgres que incrementa a cada update na linha.
        // Não é preciso adicionar coluna via migration. Em conflito, o EF lança
        // DbUpdateConcurrencyException, que EfUnitOfWork encapsula em ConcurrencyConflictException.
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion()
            .HasColumnName("xmin")
            .ValueGeneratedOnAddOrUpdate();
    }
}
