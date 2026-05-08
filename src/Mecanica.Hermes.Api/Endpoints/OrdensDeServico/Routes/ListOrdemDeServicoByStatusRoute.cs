using Mecanica.Hermes.Shared.AspNetCore.Endpoints;
using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;
using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Mappings;
using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Shared.AspNetCore.Presenter;
using Mecanica.Hermes.Shared.Core.Pagination;
using Mecanica.Hermes.Application.OrdensDeServico.Queries.ListOrdemDeServicoByStatus;
using Mecanica.Hermes.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;

public static class ListOrdemDeServicoByStatusRoute
{
    public static void MapListOrdemDeServicoByStatus(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/status/{status}", ListOrdemDeServicoByStatus)
            .WithName("ListOrdemDeServicoByStatus")
            .ProducesQueryList<PagedResult<OrdemDeServicoResponse>>()
            .RequireAuthorization(AuthPolicies.OnlyAdminScope)
            .WithDescription("Lista ordens de serviço filtradas por status com paginação");
    }

    private static async Task<IResult> ListOrdemDeServicoByStatus(
        [FromRoute] string status,
        [FromServices] ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new ListOrdemDeServicoByStatusQuery(status, page, pageSize);
        var response = await sender.Send(query, cancellationToken);

        return response.Present(pagedDto => pagedDto.ToResponse());
    }
}
