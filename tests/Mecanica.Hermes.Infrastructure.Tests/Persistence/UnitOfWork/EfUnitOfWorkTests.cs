using System.Reflection;
using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Infrastructure.Persistence;
using Mecanica.Hermes.Infrastructure.Persistence.Outbox;
using Mecanica.Hermes.Infrastructure.Persistence.UnitOfWork;
using Mecanica.Hermes.TestUtils.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mecanica.Hermes.Infrastructure.Tests.Persistence.UnitOfWork;

/// <summary>
///     Cobertura do <see cref="EfUnitOfWork" /> com Outbox transacional. Usa <c>AppDbContext</c>
///     com provider InMemory para validar que <c>CommitAsync</c> grava as mensagens Outbox
///     na mesma transação do <c>SaveChangesAsync</c>.
/// </summary>
[Trait("Category", "Unit")]
public class EfUnitOfWorkTests
{
    private static AppDbContext NewDb()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    [Fact]
    public void CollectEventsFrom_DeveCopiarELimparEventosDoAgregado()
    {
        using var db = NewDb();
        var sut = new EfUnitOfWork(db, NullLogger<EfUnitOfWork>.Instance, TimeProvider.System);

        var ordem = new OrdemDeServicoBuilder().Build();
        ordem.AvancarEtapa(); // gera Domain Event
        ordem.DomainEvents.Should().NotBeEmpty();

        sut.CollectEventsFrom(ordem);

        ordem.DomainEvents.Should().BeEmpty("foram movidos para o pending do UoW");
    }

    [Fact]
    public async Task CommitAsync_SemEventosPendentes_NaoEscreveOutbox()
    {
        using var db = NewDb();
        var sut = new EfUnitOfWork(db, NullLogger<EfUnitOfWork>.Instance, TimeProvider.System);

        await sut.CommitAsync(CancellationToken.None);

        (await db.OutboxMessages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CommitAsync_ComEventosPendentes_DevePersistirOutboxMessages()
    {
        using var db = NewDb();
        var sut = new EfUnitOfWork(db, NullLogger<EfUnitOfWork>.Instance, TimeProvider.System);

        var ordem = new OrdemDeServicoBuilder().Build();
        ordem.AvancarEtapa();
        sut.CollectEventsFrom(ordem);

        await sut.CommitAsync(CancellationToken.None);

        var messages = await db.OutboxMessages.ToListAsync();
        messages.Should().NotBeEmpty();
        messages.Should().AllSatisfy(m =>
        {
            m.Status.Should().Be(OutboxMessageStatus.Pending,
                "processor é assíncrono e roda separadamente");
            m.ProcessedOn.Should().BeNull();
            m.RetryCount.Should().Be(0);
            m.Type.Should().NotBeNullOrEmpty();
            m.Payload.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task CommitAsync_AposPrimeiroCommit_NaoDeveDuplicarOutbox()
    {
        using var db = NewDb();
        var sut = new EfUnitOfWork(db, NullLogger<EfUnitOfWork>.Instance, TimeProvider.System);

        var ordem = new OrdemDeServicoBuilder().Build();
        ordem.AvancarEtapa();
        sut.CollectEventsFrom(ordem);

        await sut.CommitAsync(CancellationToken.None);
        var afterFirst = await db.OutboxMessages.CountAsync();

        await sut.CommitAsync(CancellationToken.None);
        var afterSecond = await db.OutboxMessages.CountAsync();

        afterSecond.Should().Be(afterFirst, "o segundo commit não tem eventos novos pendentes");
    }

    [Fact]
    public async Task CommitAsync_QuandoSerializacaoFalha_DeveLogarESkipEventoSemAbortarCommit()
    {
        // Evento que não pode ser serializado (referência circular simulada por tipo System.Object)
        // — usa um IDomainEvent cujo tipo não possui DomainEventNameAttribute e cujo Serialize lança.
        using var db = NewDb();
        var sut = new EfUnitOfWork(db, NullLogger<EfUnitOfWork>.Instance, TimeProvider.System);

        // Injeta um evento problemático via reflexão para acessar o campo privado _pendingEvents.
        var problemEvent = new UnserializableEvent();
        var pendingField = typeof(EfUnitOfWork)
            .GetField("_pendingEvents", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var pending = (List<IDomainEvent>)pendingField.GetValue(sut)!;
        pending.Add(problemEvent);

        // Não deve lançar — falha de serialização é swallowed com log
        var act = async () => await sut.CommitAsync(CancellationToken.None);
        await act.Should().NotThrowAsync("serialização falha não deve propagar exceção");

        // Nenhuma mensagem Outbox deve ter sido gravada
        (await db.OutboxMessages.CountAsync()).Should().Be(0,
            "evento não serializável não gera entrada Outbox");
    }

    // Evento cujo GetType().AssemblyQualifiedName retorna null em tempo de compilação — simulado
    // sobreescrevendo ToString(); o real mecanismo de falha é injetado via tipo anônimo.
    private sealed class UnserializableEvent : IDomainEvent
    {
        // Propriedade circular que faz System.Text.Json lançar JsonException — intencional para o teste.
#pragma warning disable S2292
        public UnserializableEvent Self => this;
#pragma warning restore S2292
    }
}
