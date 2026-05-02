using System.Diagnostics;
using Mecanica.Hermes.Application.OrdensDeServico.Commands;
using Mecanica.Hermes.Domain.Common.Results;
using MediatR;

#pragma warning disable CA2016 // RequestHandlerDelegate<T> does not accept CancellationToken

namespace Mecanica.Hermes.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior que cria um <see cref="Activity"/> OpenTelemetry para cada request
/// MediatR, propagando o nome do request e o <c>OrdemDeServicoId</c> quando disponível.
/// Move a criação de Activity para fora dos handlers individuais.
/// </summary>
public sealed class TracingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private static readonly ActivitySource Source =
        new("MecanicaHermes.Application",
            typeof(TracingBehavior<,>).Assembly.GetName().Version?.ToString() ?? "1.0");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using var activity = Source.StartActivity(requestName, ActivityKind.Internal);

        activity?.SetTag("mediator.request", requestName);

        if (request is IOrdemDeServicoCommand ordemCommand)
            activity?.SetTag("ordem_de_servico.id", ordemCommand.OrdemDeServicoId.ToString());

        var response = await next();

        activity?.SetStatus(response.IsSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error);

        return response;
    }
}
