using MassTransit;
using Mecanica.Hermes.Infrastructure.MassTransit.Adapters;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mecanica.Hermes.Infrastructure.Tests.MassTransit.Adapters;

[Trait("Category", "Unit")]
public class MassTransitMessagePublisherTests
{
    [Fact]
    public async Task Publish_DeveDelegarParaIPublishEndpoint()
    {
        var endpoint = new Mock<IPublishEndpoint>();
        var captured = new List<object>();
        endpoint.Setup(e => e.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((msg, _) => captured.Add(msg))
            .Returns(Task.CompletedTask);

        var adapter = new MassTransitMessagePublisher(
            endpoint.Object,
            NullLogger<MassTransitMessagePublisher>.Instance);
        var message = new TestMessage(Guid.NewGuid());

        await adapter.Publish(message, CancellationToken.None);

        captured.Should().ContainSingle()
            .Which.Should().BeSameAs(message);
    }

    [Fact]
    public async Task Publish_QuandoBrokerFalha_DeveLancarBrokerUnavailableException()
    {
        var endpoint = new Mock<IPublishEndpoint>();
        endpoint.Setup(e => e.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker indisponível"));

        var adapter = new MassTransitMessagePublisher(
            endpoint.Object,
            NullLogger<MassTransitMessagePublisher>.Instance);

        var act = async () => await adapter.Publish(new TestMessage(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<BrokerUnavailableException>();
    }

    public sealed record TestMessage(Guid Id);
}
