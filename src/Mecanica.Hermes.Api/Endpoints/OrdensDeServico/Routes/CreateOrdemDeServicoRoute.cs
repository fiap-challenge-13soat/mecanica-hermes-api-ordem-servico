using System.Net;
using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;
using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Application.OrdensDeServico.Commands.CreateOrdemDeServico;
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

public static class CreateOrdemDeServicoRoute
{
    public static void MapCreateOrdemDeServico(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", CreateOrdemDeServico)
            .WithName("CreateOrdemDeServico")
            .ProducesAcceptedCommand<CreateOrdemDeServicoAcceptedResponse>()
            .RequireAuthorization(AuthPolicies.OnlyAdminScope)
            .WithDescription(
                "Solicita a criação de uma nova ordem de serviço (processado de forma assíncrona via SAGA). " +
                "O Id retornado já pode ser utilizado para consultar o recurso assim que o consumer " +
                "concluir a persistência.")
            .AddEndpointFilter<ValidationFilter<CreateOrdemDeServicoRequest>>();
    }

    private static async Task<IResult> CreateOrdemDeServico(
        [FromBody] CreateOrdemDeServicoRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        // O Id é gerado aqui (camada de API) para que a resposta 202 Accepted possa carregar
        // o identificador final mesmo antes do consumer ter persistido o agregado.
        var ordemDeServicoId = Guid.NewGuid();

        var command = new CreateOrdemDeServicoCommand(
            ordemDeServicoId,
            request.ClienteId,
            request.VeiculoId,
            request.ProblemaRelatado,
            request.Observacoes);

        var response = await sender.Send(command, cancellationToken);

        if (!response.IsSuccess)
            return response.Present();

        if (response.StatusCode != HttpStatusCode.Accepted)
            return response.Present();

        return Results.Accepted(
            $"/api/ordens-de-servico/{ordemDeServicoId}",
            new CreateOrdemDeServicoAcceptedResponse(ordemDeServicoId));
    }
}
