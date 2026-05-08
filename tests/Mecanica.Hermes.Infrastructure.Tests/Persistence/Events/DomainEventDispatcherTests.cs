using Mecanica.Hermes.Shared.Core.Domain;
using Mecanica.Hermes.Infrastructure.Persistence.Events;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mecanica.Hermes.Infrastructure.Tests.Persistence.Events;

[Trait("Category", "Unit")]
public class DomainEventDispatcherTests
{
    private readonly Mock<IPublisher> _publisher = new();

    private DomainEventDispatcher CreateDispatcher()
    {
        return new DomainEventDispatcher(_publisher.Object, NullLogger<DomainEventDispatcher>.Instance);
    }

    [Fact]
    public async Task DispatchAsync_DeveEnvolverEventosEmDomainEventNotificationEPublicarCadaUm()
    {
        var eventA = new EventA(Guid.NewGuid());
        var eventB = new EventB(Guid.NewGuid());
        var events = new IDomainEvent[] { eventA, eventB };

        var captured = new List<INotification>();
        _publisher.Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((n, _) => captured.Add(n))
            .Returns(Task.CompletedTask);

        await CreateDispatcher().DispatchAsync(events, CancellationToken.None);

        captured.Should().HaveCount(2);
        captured[0].Should().BeOfType<DomainEventNotification<EventA>>()
            .Which.Event.Should().Be(eventA);
        captured[1].Should().BeOfType<DomainEventNotification<EventB>>()
            .Which.Event.Should().Be(eventB);
    }

    [Fact]
    public async Task DispatchAsync_QuandoUmHandlerFalha_DeveContinuarComDemais()
    {
        var eventA = new EventA(Guid.NewGuid());
        var eventB = new EventB(Guid.NewGuid());
        var events = new IDomainEvent[] { eventA, eventB };

        var publishedSecond = false;
        _publisher.Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((n, _) =>
            {
                if (n is DomainEventNotification<EventA>)
                    throw new InvalidOperationException("boom");
                if (n is DomainEventNotification<EventB>)
                    publishedSecond = true;
            })
            .Returns(Task.CompletedTask);

        var act = async () => await CreateDispatcher().DispatchAsync(events, CancellationToken.None);

        await act.Should().NotThrowAsync("falhas por evento devem ser isoladas e não propagadas");
        publishedSecond.Should().BeTrue("segundo evento deve ser despachado mesmo após falha no primeiro");
    }

    [Fact]
    public async Task DispatchAsync_ComListaVazia_NaoDeveInvocarPublisher()
    {
        await CreateDispatcher().DispatchAsync([], CancellationToken.None);

        _publisher.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed record EventA(Guid Id) : IDomainEvent;

    private sealed record EventB(Guid Id) : IDomainEvent;
}
