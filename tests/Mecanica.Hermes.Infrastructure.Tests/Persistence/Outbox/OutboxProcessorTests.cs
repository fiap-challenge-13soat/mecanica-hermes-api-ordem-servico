using System.Text.Json;
using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Infrastructure.Persistence;
using Mecanica.Hermes.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Mecanica.Hermes.Infrastructure.Tests.Persistence.Outbox;

/// <summary>
///     Testes unitários do <see cref="OutboxProcessor" /> usando InMemory provider.
///     Exercita o caminho de fallback (sem FOR UPDATE SKIP LOCKED) para cobrir o
///     processamento de lotes, DLQ e caminhos de erro.
/// </summary>
[Trait("Category", "Unit")]
public class OutboxProcessorTests
{
    private static AppDbContext NewDb()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private static (OutboxProcessor sut, Mock<IDomainEventDispatcher> dispatcherMock, AppDbContext db) BuildSut(
        int maxRetries = 3)
    {
        var db = NewDb();

        var dispatcherMock = new Mock<IDomainEventDispatcher>();

        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(dispatcherMock.Object);
        var sp = services.BuildServiceProvider();

        var scopeFactory = new Mock<IServiceScopeFactory>();
        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(sp);
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var options = Options.Create(new OutboxOptions
        {
            BatchSize = 10,
            MaxRetries = maxRetries,
            PollInterval = TimeSpan.FromMilliseconds(50),
            AllowNonLockingFallback = true // InMemory provider — locking SQL não suportado
        });

        var metrics = new OutboxHealthMetrics();

        var sut = new OutboxProcessor(
            scopeFactory.Object,
            metrics,
            options,
            NullLogger<OutboxProcessor>.Instance,
            TimeProvider.System);

        return (sut, dispatcherMock, db);
    }

    private static OutboxMessageEntity PendingMessage(string type = "test.event.v1", string payload = "{}")
    {
        return new OutboxMessageEntity(Guid.NewGuid(), type, payload, DateTime.UtcNow);
    }

    [Fact]
    public async Task ExecuteAsync_QuandoNaoHaMensagens_NaoChamamDispatcher()
    {
        var (sut, dispatcherMock, _) = BuildSut();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await sut.StartAsync(cts.Token);
        await Task.Delay(150);
        await sut.StopAsync(CancellationToken.None);

        dispatcherMock.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessBatch_QuandoTipoNaoResolve_DeveMarcarFalha()
    {
        var (sut, _, db) = BuildSut(2);

        var msg = PendingMessage("tipo.inexistente.v99");
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await sut.StartAsync(cts.Token);
        await Task.Delay(200);
        await sut.StopAsync(CancellationToken.None);

        var saved = await db.OutboxMessages.FirstAsync();
        saved.RetryCount.Should().BeGreaterThan(0);
        saved.LastError.Should().NotBeNullOrEmpty();
        saved.Status.Should().NotBe(OutboxMessageStatus.Processed);
    }

    [Fact]
    public async Task ProcessBatch_QuandoTipoNaoResolveEExcedeLimite_DeveDeadLetter()
    {
        var (sut, _, db) = BuildSut(1);

        var msg = PendingMessage("tipo.inexistente.v99");
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        await sut.StartAsync(cts.Token);
        await Task.Delay(300);
        await sut.StopAsync(CancellationToken.None);

        var saved = await db.OutboxMessages.FirstAsync();
        saved.Status.Should().Be(OutboxMessageStatus.DeadLettered);
    }

    [Fact]
    public void OutboxMessageEntity_MarkAsFailed_DeveIncrementarRetryCount()
    {
        var msg = PendingMessage();

        msg.MarkAsFailed("erro simulado", 5);

        msg.RetryCount.Should().Be(1);
        msg.LastError.Should().Be("erro simulado");
        msg.Status.Should().Be(OutboxMessageStatus.Pending);
    }

    [Fact]
    public void OutboxMessageEntity_MarkAsFailed_QuandoExcedeLimite_DeveDeadLetter()
    {
        var msg = PendingMessage();

        msg.MarkAsFailed("err", 1);

        msg.Status.Should().Be(OutboxMessageStatus.DeadLettered);
        msg.RetryCount.Should().Be(1);
    }

    [Fact]
    public void OutboxMessageEntity_MarkAsProcessed_DeveMarcarStatusProcessed()
    {
        var msg = PendingMessage();
        var when = DateTime.UtcNow;

        msg.MarkAsProcessed(when);

        msg.Status.Should().Be(OutboxMessageStatus.Processed);
        msg.ProcessedOn.Should().BeCloseTo(when, TimeSpan.FromSeconds(1));
        msg.LastError.Should().BeNull();
    }

    [Fact]
    public async Task ProcessBatch_CaminhoFeliz_DeveMarcarStatusProcessedAposDespacho()
    {
        // Usa o discriminador real registrado em OrdemDeServicoCriadoEvent
        const string eventType = "ordem-de-servico.criado.v1";
        var evt = new OrdemDeServicoCriadoEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "problema", OrdemDeServicoStatus.Recebida);

        var (sut, dispatcherMock, db) = BuildSut(3);

        dispatcherMock
            .Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var msg = new OutboxMessageEntity(Guid.NewGuid(), eventType, JsonSerializer.Serialize(evt), DateTime.UtcNow);
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        await sut.StartAsync(cts.Token);
        await Task.Delay(300);
        await sut.StopAsync(CancellationToken.None);

        var saved = await db.OutboxMessages.FirstAsync();
        saved.Status.Should().Be(OutboxMessageStatus.Processed,
            "mensagem com tipo válido e dispatcher sem erro deve ser marcada como Processed");
        saved.ProcessedOn.Should().NotBeNull();
        saved.RetryCount.Should().Be(0);

        dispatcherMock.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
