using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Application.OrdensDeServico.Commands.RemoveServico;
using Mecanica.Hermes.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;

public static class RemoveServicoRoute
{
    public static void MapRemoveServico(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/{id:guid}/servicos/{servicoId:guid}", RemoveServico)
            .WithName("RemoveServicoFromOrdemDeServico")
            .ProducesAcceptedCommand()
            .RequireAuthorization(AuthPolicies.OnlyAdminScope)
            .WithDescription(
                "Solicita remoção de um serviço da ordem de serviço (processado de forma assíncrona via SAGA)");
    }

    private static async Task<IResult> RemoveServico(
        [FromRoute] Guid id,
        [FromRoute] Guid servicoId,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RemoveServicoCommand(id, servicoId);
        var response = await sender.Send(command, cancellationToken);

        return response.Present();
    }
}
