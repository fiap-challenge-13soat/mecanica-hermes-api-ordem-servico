using System.Net;
using Mecanica.Hermes.Domain.Common.Results;
using Microsoft.AspNetCore.Http;

namespace Mecanica.Hermes.Api.Presenter;

public static class ResultPresenterExtensions
{
    /// <summary>
    /// Maps a successful <see cref="Result{TApp}"/> to its API contract using the provided
    /// converter function, returning OK or a custom success result. Uses manual mapping instead
    /// of AutoMapper to eliminate the library dependency.
    /// </summary>
    public static IResult Present<TApp, TApi>(
        this Result<TApp> result,
        Func<TApp, TApi> map,
        Func<TApi, IResult>? success = null)
    {
        if (result.IsFailure)
            return result.StatusCode switch
            {
                HttpStatusCode.NoContent => Results.NoContent(),
                HttpStatusCode.NotFound => Results.NotFound(),
                HttpStatusCode.Conflict => result.AsConflictResult(),
                HttpStatusCode.Unauthorized => Results.Unauthorized(),
                HttpStatusCode.BadRequest => result.AsBadRequestResult(),
                _ => result.AsInternalServerErrorResult()
            };

        var apiModel = map(result.Data!);

        if (success != null)
            return success(apiModel);

        return Results.Ok(apiModel);
    }

    public static IResult Present<TApp>(this Result<TApp> result)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return result.StatusCode switch
        {
            HttpStatusCode.NoContent => Results.NoContent(),
            HttpStatusCode.NotFound => Results.NotFound(),
            HttpStatusCode.Conflict => result.AsConflictResult(),
            HttpStatusCode.Unauthorized => Results.Unauthorized(),
            HttpStatusCode.BadRequest => result.AsBadRequestResult(),
            _ => result.AsInternalServerErrorResult()
        };
    }

    public static IResult Present(this Result result)
    {
        if (result.IsSuccess)
            return result.StatusCode == HttpStatusCode.Accepted
                ? Results.Accepted()
                : Results.NoContent();

        return result.StatusCode switch
        {
            HttpStatusCode.NoContent => Results.NoContent(),
            HttpStatusCode.NotFound => Results.NotFound(),
            HttpStatusCode.Conflict => result.AsConflictResult(),
            HttpStatusCode.Unauthorized => Results.Unauthorized(),
            HttpStatusCode.BadRequest => result.AsBadRequestResult(),
            _ => result.AsInternalServerErrorResult()
        };
    }

    private static IResult AsConflictResult<TApp>(this Result<TApp> result)
    {
        var problemDetails = new ApiProblemDetails
        {
            Title = "Conflito.",
            Status = (int)HttpStatusCode.Conflict,
            Detail = "Registro já cadastrado na base da dados.",
            Errors = result.Errors
        };

        return Results.Conflict(problemDetails);
    }

    private static IResult AsConflictResult(this Result result)
    {
        var problemDetails = new ApiProblemDetails
        {
            Title = "Conflito.",
            Status = (int)HttpStatusCode.Conflict,
            Detail = "Registro já cadastrado na base da dados.",
            Errors = result.Errors
        };

        return Results.Conflict(problemDetails);
    }

    private static IResult AsBadRequestResult<TApp>(this Result<TApp> result)
    {
        var problemDetails = new ApiProblemDetails
        {
            Title = "Requisição Inválida.",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "A requisição contém parâmetros inválidos.",
            Errors = result.Errors
        };

        return Results.BadRequest(problemDetails);
    }

    private static IResult AsBadRequestResult(this Result result)
    {
        var problemDetails = new ApiProblemDetails
        {
            Title = "Requisição Inválida.",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "A requisição contém parâmetros inválidos.",
            Errors = result.Errors
        };

        return Results.BadRequest(problemDetails);
    }

    private static IResult AsInternalServerErrorResult<TApp>(this Result<TApp> result)
    {
        var problemDetails = new ApiProblemDetails
        {
            Title = "Erro Interno.",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = "Ocorreu um erro inesperado no servidor.",
            Errors = result.Errors
        };

        return Results.Problem(problemDetails);
    }

    private static IResult AsInternalServerErrorResult(this Result result)
    {
        var problemDetails = new ApiProblemDetails
        {
            Title = "Erro Interno.",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = "Ocorreu um erro inesperado no servidor.",
            Errors = result.Errors
        };

        return Results.Problem(problemDetails);
    }
}