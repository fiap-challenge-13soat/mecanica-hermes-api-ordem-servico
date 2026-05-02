using Mecanica.Hermes.Api.Presenter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Mecanica.Hermes.Api.Endpoints;

/// <summary>
///     Convenções compartilhadas entre todos os endpoints da API. Concentra a documentação
///     de status codes comuns (<c>ApiProblemDetails</c>) para reduzir duplicação por rota.
/// </summary>
internal static class EndpointConventions
{
    /// <summary>
    ///     Adiciona os <c>Produces&lt;ApiProblemDetails&gt;</c> esperados em qualquer endpoint
    ///     autenticado: 400, 401, 403, 500.
    /// </summary>
    public static RouteHandlerBuilder ProducesStandardErrors(this RouteHandlerBuilder builder)
    {
        return builder
            .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ApiProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ApiProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ApiProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    ///     Convenção para endpoints que disparam um command assíncrono (SAGA): retorna 202 Accepted
    ///     + lista padrão de erros.
    /// </summary>
    public static RouteHandlerBuilder ProducesAcceptedCommand(this RouteHandlerBuilder builder)
    {
        return builder
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardErrors();
    }

    /// <summary>
    ///     Convenção para endpoints de criação assíncrona que devolvem um corpo com o Id gerado.
    /// </summary>
    public static RouteHandlerBuilder ProducesAcceptedCommand<TAcceptedBody>(this RouteHandlerBuilder builder)
    {
        return builder
            .Produces<TAcceptedBody>(StatusCodes.Status202Accepted)
            .ProducesStandardErrors();
    }

    /// <summary>
    ///     Convenção para endpoints de consulta de um único recurso: 200 com body, 404, padrão de erros.
    /// </summary>
    public static RouteHandlerBuilder ProducesQuerySingle<TBody>(this RouteHandlerBuilder builder)
    {
        return builder
            .Produces<TBody>()
            .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound)
            .ProducesStandardErrors();
    }

    /// <summary>
    ///     Convenção para endpoints de listagem paginada.
    /// </summary>
    public static RouteHandlerBuilder ProducesQueryList<TBody>(this RouteHandlerBuilder builder)
    {
        return builder
            .Produces<TBody>()
            .ProducesStandardErrors();
    }
}
