using Mecanica.Hermes.Api.Endpoints;
using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Application.OrdensDeServico.Commands.AvancarEtapa;
using Mecanica.Hermes.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;

public static class AvancarEtapaRoute
{
    public static void MapAvancarEtapa(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/{id:guid}/avancar", AvancarEtapa)
            .WithName("AvancarEtapa")
            .ProducesAcceptedCommand()
            .RequireAuthorization(AuthPolicies.OnlyAdminScope)
            .WithDescription("Solicita avanço da ordem de serviço para a próxima etapa (processado de forma assíncrona via SAGA)");
    }

    private static async Task<IResult> AvancarEtapa(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AvancarEtapaCommand(id);
        var response = await sender.Send(command, cancellationToken);

        return response.Present();
    }
}
