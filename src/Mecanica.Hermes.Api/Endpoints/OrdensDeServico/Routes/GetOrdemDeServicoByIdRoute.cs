using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;
using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Mappings;
using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Application.OrdensDeServico.Queries.GetOrdemDeServicoById;
using Mecanica.Hermes.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;

public static class GetOrdemDeServicoByIdRoute
{
    public static void MapGetOrdemDeServicoById(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{id:guid}", GetOrdemDeServicoById)
            .WithName("GetOrdemDeServicoById")
            .ProducesQuerySingle<OrdemDeServicoResponse>()
            .RequireAuthorization(AuthPolicies.AllowClienteScope)
            .WithDescription("Busca uma ordem de serviço por ID");
    }

    private static async Task<IResult> GetOrdemDeServicoById(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetOrdemDeServicoByIdQuery(id);
        var response = await sender.Send(query, cancellationToken);

        return response.Present(dto => dto.ToResponse());
    }
}
