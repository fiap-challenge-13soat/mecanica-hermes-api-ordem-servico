using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Mecanica.Hermes.Api.Filters;

/// <summary>
///     Endpoint filter genérico que executa FluentValidation antes de invocar o handler.
///     Retorna RFC 7807 400 Bad Request com os erros de validação quando inválido.
/// </summary>
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return await next(context);

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Requisição Inválida.",
                detail: string.Join(" ", errors),
                extensions: new Dictionary<string, object?> { ["errors"] = errors });
        }

        return await next(context);
    }
}
