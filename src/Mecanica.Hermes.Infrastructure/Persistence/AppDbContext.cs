using System.Reflection;
using Mecanica.Hermes.Infrastructure.Persistence.Entities;
using Mecanica.Hermes.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Mecanica.Hermes.Infrastructure.Persistence;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<OrdemDeServicoEntity> OrdensDeServico => Set<OrdemDeServicoEntity>();
    public DbSet<OrdemDeServicoStatusEntity> OrdemDeServicoStatus => Set<OrdemDeServicoStatusEntity>();

    public DbSet<OrdemDeServicoHistoricoStatusEntity> OrdemDeServicoHistoricoStatus =>
        Set<OrdemDeServicoHistoricoStatusEntity>();

    public DbSet<OrdemDeServicoProdutoEntity> OrdemDeServicoProdutos => Set<OrdemDeServicoProdutoEntity>();
    public DbSet<OrdemDeServicoServicoEntity> OrdemDeServicoServicos => Set<OrdemDeServicoServicoEntity>();
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Extensões específicas de PostgreSQL — só aplicáveis quando o provider for Npgsql.
        // Em testes com provider InMemory/SQLite, o trecho é ignorado para permitir uso
        // do mesmo modelo sem dependência do banco real.
        if (Database.IsNpgsql())
        {
            modelBuilder.HasPostgresExtension("pg_trgm");
            modelBuilder.HasPostgresExtension("unaccent");
        }

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
