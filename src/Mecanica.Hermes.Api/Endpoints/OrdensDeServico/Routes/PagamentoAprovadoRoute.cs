using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Application.OrdensDeServico.Commands.PagamentoAprovado;
using Mecanica.Hermes.Infrastructure.Settings;
using Mecanica.Hermes.Shared.AspNetCore.Endpoints;
using Mecanica.Hermes.Shared.AspNetCore.Presenter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;

public static class PagamentoAprovadoRoute
{
    public static void MapPagamentoAprovado(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/{id:guid}/pagamento-aprovado", PagamentoAprovado)
            .WithName("PagamentoAprovadoOrdemDeServico")
            .ProducesAcceptedCommand()
            .RequireAuthorization(AuthPolicies.OnlyAdminScope)
            .WithDescription(
                "Confirma o pagamento da ordem de serviço (processado de forma assíncrona via SAGA). " +
                "Exige status AguardandoPagamento.");
    }

    private static async Task<IResult> PagamentoAprovado(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new PagamentoAprovadoCommand(id);
        var response = await sender.Send(command, cancellationToken);

        return response.Present();
    }
}
