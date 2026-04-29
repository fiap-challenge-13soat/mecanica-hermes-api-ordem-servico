using Mecanica.Hermes.Api.Endpoints;
using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;
using Mecanica.Hermes.Api.Filters;
using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Application.OrdensDeServico.Commands.AddProduto;
using Mecanica.Hermes.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;

public static class AddProdutoRoute
{
    public static void MapAddProduto(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{id:guid}/produtos", AddProduto)
            .WithName("AddProdutoToOrdemDeServico")
            .ProducesAcceptedCommand()
            .RequireAuthorization(AuthPolicies.OnlyAdminScope)
            .WithDescription("Solicita adição de um produto à ordem de serviço (processado de forma assíncrona via SAGA)")
            .AddEndpointFilter<ValidationFilter<AddOrdemDeServicoProdutoRequest>>();
    }

    private static async Task<IResult> AddProduto(
        [FromRoute] Guid id,
        [FromBody] AddOrdemDeServicoProdutoRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AddProdutoCommand(id, request.ProdutoId, request.ValorUnitario, request.Quantidade);
        var response = await sender.Send(command, cancellationToken);

        return response.Present();
    }
}
