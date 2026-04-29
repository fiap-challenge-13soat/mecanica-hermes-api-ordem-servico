namespace Mecanica.Hermes.Application.Common.Messaging;

/// <summary>
/// Port (Hexagonal) que abstrai a publicação assíncrona de mensagens em um broker.
/// A implementação atual delega para MassTransit (<c>IPublishEndpoint</c>), mas a Application
/// permanece independente dessa escolha, podendo ser portada para outro adapter (NServiceBus,
/// Wolverine, Brighter) sem mudanças aqui.
/// </summary>
public interface IAsyncMessagePublisher
{
    /// <summary>
    /// Publica uma mensagem no broker. O tipo concreto da mensagem (record) é usado para
    /// roteamento. Não retorna confirmação explícita — entrega é responsabilidade do adapter
    /// (outbox in-memory garante "all-or-nothing" no escopo do consumer).
    /// </summary>
    Task Publish(object message, CancellationToken cancellationToken);
}
