using Mecanica.Hermes.Application.Common.Pagination;
using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Application.OrdensDeServico.Ports;

/// <summary>
/// Port (Hexagonal) de leitura para o agregado <see cref="OrdemDeServico"/>.
/// Aplicação do princípio ISP: Query Handlers não devem conhecer mutações de escrita.
/// Permite, no futuro, apontar para uma read replica sem afetar a escrita (CQRS).
/// </summary>
public interface IOrdemDeServicoReader
{
    Task<OrdemDeServico?> GetByIdAsync(Guid id);
    Task<PagedResult<OrdemDeServico>> ListByStatusAsync(OrdemDeServicoStatus status, PaginationParams paginationParams);
    Task<PagedResult<OrdemDeServico>> ListAllAsync(PaginationParams paginationParams);
}
