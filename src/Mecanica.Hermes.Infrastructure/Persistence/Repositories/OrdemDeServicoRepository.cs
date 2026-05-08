using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Infrastructure.Persistence.Mappings;
using Mecanica.Hermes.Shared.Core.Pagination;
using Mecanica.Hermes.Shared.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mecanica.Hermes.Infrastructure.Persistence.Repositories;

internal class OrdemDeServicoRepository(AppDbContext db, IUnitOfWork uow) : IOrdemDeServicoRepository
{
    public async Task<OrdemDeServico?> GetByIdAsync(Guid id)
    {
        var entity = await db.OrdensDeServico
            .AsNoTracking()
            .Include(o => o.StatusAtual)
            .Include(o => o.HistoricosStatus)
            .Include(o => o.Produtos)
            .Include(o => o.Servicos)
            .FirstOrDefaultAsync(o => o.Id == id);

        return entity?.ToDomain();
    }

    public async Task<PagedResult<OrdemDeServico>> ListByStatusAsync(OrdemDeServicoStatus status,
        PaginationParams paginationParams)
    {
        var totalCount = await db.OrdensDeServico
            .Where(o => o.StatusAtual.StatusAtual == status)
            .CountAsync();

        var entities = await db.OrdensDeServico
            .AsNoTracking()
            .Include(o => o.StatusAtual)
            .Include(o => o.HistoricosStatus)
            .Include(o => o.Produtos)
            .Include(o => o.Servicos)
            .Where(o => o.StatusAtual.StatusAtual == status)
            .OrderBy(o => o.CreatedAt)
            .Skip(paginationParams.Skip)
            .Take(paginationParams.PageSize)
            .ToListAsync();

        var domain = entities.Select(e => e.ToDomain()).ToList();

        return PagedResult<OrdemDeServico>.Create(
            domain,
            paginationParams.Page,
            paginationParams.PageSize,
            totalCount);
    }

    public async Task<PagedResult<OrdemDeServico>> ListAllAsync(PaginationParams paginationParams)
    {
        var totalCount = await db.OrdensDeServico.CountAsync();

        var entities = await db.OrdensDeServico
            .AsNoTracking()
            .Include(o => o.StatusAtual)
            .Include(o => o.HistoricosStatus)
            .Include(o => o.Produtos)
            .Include(o => o.Servicos)
            .OrderBy(o => o.CreatedAt)
            .Skip(paginationParams.Skip)
            .Take(paginationParams.PageSize)
            .ToListAsync();

        var domain = entities.Select(e => e.ToDomain()).ToList();

        return PagedResult<OrdemDeServico>.Create(
            domain,
            paginationParams.Page,
            paginationParams.PageSize,
            totalCount);
    }

    public async Task AddAsync(OrdemDeServico ordemDeServico)
    {
        var entity = ordemDeServico.ToEntity();
        await db.OrdensDeServico.AddAsync(entity);
        // O Id é definido na construção do agregado (OrdemDeServico.Criar);
        // ToEntity copia esse mesmo Id, então não há necessidade de sincronizar de volta.
        uow.CollectEventsFrom(ordemDeServico);
    }

    public async Task UpdateAsync(OrdemDeServico ordemDeServico)
    {
        var entity = ordemDeServico.ToEntity();

        // GetByIdAsync usa AsNoTracking(), então a coluna de sistema xmin do PostgreSQL
        // nunca é carregada no round-trip entity → domain. Sem o xmin original, o EF Core
        // gera UPDATE … WHERE xmin = 0, que nunca casa. Recarrega o xmin aqui (lookup
        // pela PK) para que o check de concorrência passe.
        var xmin = await db.OrdensDeServico
            .Where(o => o.Id == entity.Id)
            .Select(o => EF.Property<uint>(o, "xmin"))
            .FirstOrDefaultAsync();

        db.OrdensDeServico.Update(entity);
        db.Entry(entity).Property<uint>("xmin").OriginalValue = xmin;

        uow.CollectEventsFrom(ordemDeServico);
    }
}
