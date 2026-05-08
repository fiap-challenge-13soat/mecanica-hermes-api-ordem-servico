using Mecanica.Hermes.Shared.Core.Pagination;
using Mecanica.Hermes.Application.OrdensDeServico.Dtos;
using Mecanica.Hermes.Application.OrdensDeServico.Mappings;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Shared.Core.Results;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using MediatR;

namespace Mecanica.Hermes.Application.OrdensDeServico.Queries.ListOrdemDeServicoByStatus;

internal sealed class ListOrdemDeServicoByStatusQueryHandler(IOrdemDeServicoReader repository)
    : IRequestHandler<ListOrdemDeServicoByStatusQuery, Result<PagedResult<OrdemDeServicoDto>>>
{
    public async Task<Result<PagedResult<OrdemDeServicoDto>>> Handle(
        ListOrdemDeServicoByStatusQuery request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrdemDeServicoStatus>(request.Status, true, out var status))
            return Result<PagedResult<OrdemDeServicoDto>>.BadRequest("Status inválido.");

        var paginationParams = new PaginationParams { Page = request.Page, PageSize = request.PageSize };

        var pagedOrdensDeServico = await repository.ListByStatusAsync(status, paginationParams);

        var ordemDeServicoDtos = pagedOrdensDeServico.Items.Select(o => o.ToDto()).ToList();

        var pagedResult = PagedResult<OrdemDeServicoDto>.Create(
            ordemDeServicoDtos,
            pagedOrdensDeServico.Page,
            pagedOrdensDeServico.PageSize,
            pagedOrdensDeServico.TotalCount);

        return Result<PagedResult<OrdemDeServicoDto>>.Ok(pagedResult);
    }
}
