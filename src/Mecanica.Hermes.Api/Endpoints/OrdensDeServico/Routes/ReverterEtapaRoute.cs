using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Application.OrdensDeServico.Commands.ReverterEtapa;
using Mecanica.Hermes.Infrastructure.Settings;
using Mecanica.Hermes.Shared.AspNetCore.Endpoints;
using Mecanica.Hermes.Shared.AspNetCore.Presenter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;

public static class ReverterEtapaRoute
{
    public static void MapReverterEtapa(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/{id:guid}/reverter", ReverterEtapa)
            .WithName("ReverterEtapa")
            .ProducesAcceptedCommand()
            .RequireAuthorization(AuthPolicies.OnlyAdminScope)
            .WithDescription(
                "Solicita reversão da ordem de serviço para a etapa anterior " +
                "(processado de forma assíncrona via SAGA)");
    }

    private static async Task<IResult> ReverterEtapa(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ReverterEtapaCommand(id);
        var response = await sender.Send(command, cancellationToken);

        return response.Present();
    }
}
