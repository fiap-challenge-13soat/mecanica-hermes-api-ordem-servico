using Mecanica.Hermes.Shared.AspNetCore.Endpoints;
using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Shared.AspNetCore.Presenter;
using Mecanica.Hermes.Application.OrdensDeServico.Commands.RejeitarOrdemDeServico;
using Mecanica.Hermes.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;

public static class RejeitarOrdemDeServicoRoute
{
    public static void MapRejeitarOrdemDeServico(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/{id:guid}/rejeitar", RejeitarOrdemDeServico)
            .WithName("RejeitarOrdemDeServico")
            .ProducesAcceptedCommand()
            .RequireAuthorization(AuthPolicies.AllowClienteScope)
            .WithDescription("Solicita rejeição da ordem de serviço (processado de forma assíncrona via SAGA)");
    }

    private static async Task<IResult> RejeitarOrdemDeServico(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RejeitarOrdemDeServicoCommand(id);
        var response = await sender.Send(command, cancellationToken);

        return response.Present();
    }
}
