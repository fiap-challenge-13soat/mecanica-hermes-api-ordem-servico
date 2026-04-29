using Mecanica.Hermes.Domain.Common.Results;
using MediatR;

namespace Mecanica.Hermes.Application.Common.Messaging;

/// <summary>
/// Handler genérico que processa qualquer <see cref="IAsyncCommand"/>:
/// <list type="number">
///   <item>publica a mensagem retornada por <see cref="IAsyncCommand.ToMessage"/> via
///   <see cref="IAsyncMessagePublisher"/> (port hexagonal);</item>
///   <item>retorna <see cref="Result.Accepted"/> indicando que o trabalho foi enfileirado.</item>
/// </list>
///
/// Logging e tracing são delegados a <c>LoggingBehavior</c> e <c>TracingBehavior</c> no pipeline.
/// Validação estrutural de IDs é feita por <c>OrdemDeServicoCommandValidationFilter</c> no lado
/// consumidor e pelos validators de edge (FluentValidation) no lado produtor.
///
/// Substitui os antigos handlers concretos (um por command) que tinham todos a mesma forma.
/// Funciona graças a <c>cfg.RegisterGenericHandlers = true</c> no registro do MediatR,
/// que resolve <c>IRequestHandler&lt;TRequest, Result&gt;</c> para qualquer
/// <typeparamref name="TRequest"/> que implemente <see cref="IAsyncCommand"/>.
/// </summary>
public sealed class AsyncCommandHandler<TRequest>(IAsyncMessagePublisher publisher)
    : IRequestHandler<TRequest, Result>
    where TRequest : IAsyncCommand
{
    public async Task<Result> Handle(TRequest request, CancellationToken cancellationToken)
    {
        await publisher.Publish(request.ToMessage(), cancellationToken);
        return Result.Accepted();
    }
}
