using Mecanica.Hermes.Application.OrdensDeServico.Dtos;
using Mecanica.Hermes.Application.OrdensDeServico.Mappings;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Application.Common.Pagination;
using Mecanica.Hermes.Domain.Common.Results;
using MediatR;

namespace Mecanica.Hermes.Application.OrdensDeServico.Queries.ListAllOrdensDeServico;

internal sealed class ListAllOrdensDeServicoQueryHandler(IOrdemDeServicoReader repository)
    : IRequestHandler<ListAllOrdensDeServicoQuery, Result<PagedResult<OrdemDeServicoDto>>>
{
    public async Task<Result<PagedResult<OrdemDeServicoDto>>> Handle(
        ListAllOrdensDeServicoQuery request,
        CancellationToken cancellationToken)
    {
        var paginationParams = new PaginationParams { Page = request.Page, PageSize = request.PageSize };
        
        var pagedOrdensDeServico = await repository.ListAllAsync(paginationParams);
        
        var ordemDeServicoDtos = pagedOrdensDeServico.Items.Select(o => o.ToDto()).ToList();
        
        var pagedResult = PagedResult<OrdemDeServicoDto>.Create(
            ordemDeServicoDtos,
            pagedOrdensDeServico.Page,
            pagedOrdensDeServico.PageSize,
            pagedOrdensDeServico.TotalCount);

        return Result<PagedResult<OrdemDeServicoDto>>.Ok(pagedResult);
    }
}
