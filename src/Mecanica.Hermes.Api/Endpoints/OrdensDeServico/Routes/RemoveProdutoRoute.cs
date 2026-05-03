using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Application.OrdensDeServico.Commands.RemoveProduto;
using Mecanica.Hermes.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;

public static class RemoveProdutoRoute
{
    public static void MapRemoveProduto(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/{id:guid}/produtos/{produtoId:guid}", RemoveProduto)
            .WithName("RemoveProduto")
            .ProducesAcceptedCommand()
            .RequireAuthorization(AuthPolicies.OnlyAdminScope)
            .WithDescription(
                "Solicita remoção de um produto da ordem de serviço (processado de forma assíncrona via SAGA)");
    }

    private static async Task<IResult> RemoveProduto(
        [FromRoute] Guid id,
        [FromRoute] Guid produtoId,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RemoveProdutoCommand(id, produtoId);
        var response = await sender.Send(command, cancellationToken);

        return response.Present();
    }
}
