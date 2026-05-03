using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Application.OrdensDeServico.Commands.CancelarOrdemDeServico;
using Mecanica.Hermes.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;

public static class CancelarOrdemDeServicoRoute
{
    public static void MapCancelarOrdemDeServico(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/{id:guid}/cancelar", CancelarOrdemDeServico)
            .WithName("CancelarOrdemDeServico")
            .ProducesAcceptedCommand()
            .RequireAuthorization(AuthPolicies.OnlyAdminScope)
            .WithDescription("Solicita cancelamento da ordem de serviço (processado de forma assíncrona via SAGA)");
    }

    private static async Task<IResult> CancelarOrdemDeServico(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CancelarOrdemDeServicoCommand(id);
        var response = await sender.Send(command, cancellationToken);

        return response.Present();
    }
}
