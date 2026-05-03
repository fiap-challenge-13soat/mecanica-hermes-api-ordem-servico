using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Application.OrdensDeServico.Commands.AprovarOrdemDeServico;
using Mecanica.Hermes.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;

public static class AprovarOrdemDeServicoRoute
{
    public static void MapAprovarOrdemDeServico(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/{id:guid}/aprovar", AprovarOrdemDeServico)
            .WithName("AprovarOrdemDeServico")
            .ProducesAcceptedCommand()
            .RequireAuthorization(AuthPolicies.AllowClienteScope)
            .WithDescription("Solicita aprovação da ordem de serviço (processado de forma assíncrona via SAGA)");
    }

    private static async Task<IResult> AprovarOrdemDeServico(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AprovarOrdemDeServicoCommand(id);
        var response = await sender.Send(command, cancellationToken);

        return response.Present();
    }
}
