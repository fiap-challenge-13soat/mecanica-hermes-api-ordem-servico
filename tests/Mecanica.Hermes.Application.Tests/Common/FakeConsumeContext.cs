using MassTransit;

namespace Mecanica.Hermes.Application.Tests.Common;

/// <summary>
/// Implementação mínima de <c>ConsumeContext&lt;T&gt;</c> via Moq, suficiente para cobrir
/// os membros efetivamente usados pelos consumers sob teste: <c>Message</c>,
/// <c>CancellationToken</c> e <c>Publish(object)</c>.
///
/// Use esta classe quando quiser invocar <c>Consume(context)</c> diretamente — sem o custo
/// de um <c>InMemoryTestHarness</c>. Opcionalmente forneça um callback de publicação para
/// capturar mensagens publicadas pelo consumer.
/// </summary>
internal static class FakeConsumeContext
{
    public static ConsumeContext<T> For<T>(T message, Action<object>? onPublish = null) where T : class
    {
        var mock = new Mock<ConsumeContext<T>>();
        mock.SetupGet(c => c.Message).Returns(message);
        mock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        // A extensão Publish(object) dispara internamente Publish<T>(T, CancellationToken).
        // Mockamos a sobrecarga não-genérica com object — o MassTransit usa dynamic dispatch.
        mock.Setup(c => c.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback((object msg, CancellationToken _) => onPublish?.Invoke(msg))
            .Returns(Task.CompletedTask);

        return mock.Object;
    }
}
