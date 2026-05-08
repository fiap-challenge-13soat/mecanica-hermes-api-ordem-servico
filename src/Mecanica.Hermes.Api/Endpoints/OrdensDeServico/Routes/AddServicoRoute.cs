using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;
using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Application.OrdensDeServico.Commands.AddServico;
using Mecanica.Hermes.Infrastructure.Settings;
using Mecanica.Hermes.Shared.AspNetCore.Endpoints;
using Mecanica.Hermes.Shared.AspNetCore.Filters;
using Mecanica.Hermes.Shared.AspNetCore.Presenter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;

public static class AddServicoRoute
{
    public static void MapAddServico(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{id:guid}/servicos", AddServico)
            .WithName("AddServicoToOrdemDeServico")
            .ProducesAcceptedCommand()
            .RequireAuthorization(AuthPolicies.OnlyAdminScope)
            .WithDescription(
                "Solicita adição de um serviço à ordem de serviço (processado de forma assíncrona via SAGA)")
            .AddEndpointFilter<ValidationFilter<AddOrdemDeServicoServicoRequest>>();
    }

    private static async Task<IResult> AddServico(
        [FromRoute] Guid id,
        [FromBody] AddOrdemDeServicoServicoRequest servicoRequest,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AddServicoCommand(id, servicoRequest.ServicoId, servicoRequest.Valor);
        var response = await sender.Send(command, cancellationToken);

        return response.Present();
    }
}
