using Mecanica.Hermes.Domain.Common.Results;
using MediatR;

namespace Mecanica.Hermes.Application.Common.Messaging;

/// <summary>
/// Comando assíncrono — representa uma intenção de mudança de estado que deve ser
/// despachada via mensageria (MassTransit) em vez de executada inline. Cada implementação
/// converte-se em uma mensagem (via <see cref="ToMessage"/>) que é publicada no broker
/// e processada por um consumer dedicado.
///
/// O caso de uso ("publicar mensagem") é genérico e fica no
/// <c>AsyncCommandHandler&lt;TRequest&gt;</c>; cada Command só precisa expor o mapeamento
/// para o contrato de mensageria correspondente.
/// </summary>
public interface IAsyncCommand : IRequest<Result>
{
    /// <summary>
    /// Converte o Command da camada de Application no contrato de mensageria publicado
    /// no broker. Tipicamente um <c>record</c> da pasta <c>Messages/Requests</c>.
    /// </summary>
    object ToMessage();
}
