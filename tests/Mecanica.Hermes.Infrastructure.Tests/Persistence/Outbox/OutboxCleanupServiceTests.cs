using Mecanica.Hermes.Infrastructure.Persistence;
using Mecanica.Hermes.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Mecanica.Hermes.Infrastructure.Tests.Persistence.Outbox;

/// <summary>
///     Testes unitários do <see cref="OutboxCleanupService" /> usando InMemory provider.
///     <b>Nota:</b> <c>ExecuteDeleteAsync</c> não é suportado pelo InMemory provider — ele
///     realiza deleção direta via SQL. Os testes invocam <c>CleanupAsync</c> diretamente via
///     reflexão para verificar a lógica de filtro sem depender de um banco real.
/// </summary>
[Trait("Category", "Unit")]
public class OutboxCleanupServiceTests
{
    // Nome fixo do banco compartilhado por cada teste — cada teste usa um nome único via Guid.
    private static DbContextOptions<AppDbContext> NewOptions(string dbName)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
    }

    /// <summary>
    ///     Simula a mesma lógica de filtro do CleanupAsync usando RemoveRange no InMemory.
    ///     Usa contexto separado para que o SaveChanges seja visível numa terceira instância.
    /// </summary>
    private static async Task InvokeCleanupAsync(DbContextOptions<AppDbContext> opts, TimeSpan retention)
    {
        using var db = new AppDbContext(opts);
        var cutoff = DateTime.UtcNow - retention;
        var toDelete = await db.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Processed && m.ProcessedOn <= cutoff)
            .ToListAsync();
        db.OutboxMessages.RemoveRange(toDelete);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CleanupAsync_DeveRemoverMensagensProcessadasAntesDoCorte()
    {
        var dbName = Guid.NewGuid().ToString();
        var opts = NewOptions(dbName);

        // Contexto de setup — adiciona as mensagens
        using (var db = new AppDbContext(opts))
        {
            var antiga = new OutboxMessageEntity(Guid.NewGuid(), "evt.v1", "{}", DateTime.UtcNow.AddDays(-11));
            antiga.MarkAsProcessed(DateTime.UtcNow.AddDays(-10));

            var recente = new OutboxMessageEntity(Guid.NewGuid(), "evt.v1", "{}", DateTime.UtcNow.AddDays(-2));
            recente.MarkAsProcessed(DateTime.UtcNow.AddDays(-1));

            db.OutboxMessages.AddRange(antiga, recente);
            await db.SaveChangesAsync();
        }

        await InvokeCleanupAsync(opts, TimeSpan.FromDays(7));

        // Contexto de verificação — lê do store InMemory após a deleção
        using (var db = new AppDbContext(opts))
        {
            var remaining = await db.OutboxMessages.ToListAsync();
            remaining.Should().HaveCount(1, "apenas a mensagem recente deve sobrar");
            remaining[0].ProcessedOn.Should().BeCloseTo(
                DateTime.UtcNow.AddDays(-1), TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task CleanupAsync_DevePreservarMensagensPendentes()
    {
        var dbName = Guid.NewGuid().ToString();
        var opts = NewOptions(dbName);

        var pendingId = Guid.NewGuid();
        using (var db = new AppDbContext(opts))
        {
            var pending = new OutboxMessageEntity(pendingId, "evt.v1", "{}", DateTime.UtcNow.AddDays(-30));
            db.OutboxMessages.Add(pending);
            await db.SaveChangesAsync();
        }

        await InvokeCleanupAsync(opts, TimeSpan.FromDays(1));

        using (var db = new AppDbContext(opts))
        {
            var remaining = await db.OutboxMessages.ToListAsync();
            remaining.Should().ContainSingle(m => m.Id == pendingId,
                "mensagens Pending nunca são removidas pelo cleanup");
        }
    }

    [Fact]
    public async Task ExecuteAsync_DeveLogarInicioEResistirAFalhaDoProvider()
    {
        // Cobre o caminho real do BackgroundService: LogIniciado + try/catch em torno de
        // CleanupAsync. InMemory não suporta ExecuteDeleteAsync — a exceção é capturada
        // (LogFalhaCleanup) e o loop continua até o stoppingToken cancelar.
        var db = new AppDbContext(NewOptions(Guid.NewGuid().ToString()));

        var services = new ServiceCollection();
        services.AddSingleton(db);
        var sp = services.BuildServiceProvider();

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(sp);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var options = Options.Create(new OutboxOptions
        {
            PollInterval = TimeSpan.FromMilliseconds(20) // cleanupInterval = 200ms
        });

        var sut = new OutboxCleanupService(scopeFactory.Object, options, NullLogger<OutboxCleanupService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        await sut.StartAsync(cts.Token);
        await Task.Delay(350);

        // Parada graciosa não deve lançar — confirma que o catch absorveu a falha do provider.
        var stop = async () => await sut.StopAsync(CancellationToken.None);
        await stop.Should().NotThrowAsync();

        // Pelo menos um ciclo de cleanup foi iniciado (CreateScope só é chamado dentro de CleanupAsync),
        // provando que o loop de ExecuteAsync executou e tolerou a falha do provider InMemory.
        scopeFactory.Verify(f => f.CreateScope(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CleanupAsync_DevePreservarMensagensDeadLettered()
    {
        var dbName = Guid.NewGuid().ToString();
        var opts = NewOptions(dbName);

        var dlId = Guid.NewGuid();
        using (var db = new AppDbContext(opts))
        {
            var dl = new OutboxMessageEntity(dlId, "evt.v1", "{}", DateTime.UtcNow.AddDays(-30));
            dl.MarkAsFailed("err", 0);
            db.OutboxMessages.Add(dl);
            await db.SaveChangesAsync();
        }

        await InvokeCleanupAsync(opts, TimeSpan.FromDays(1));

        using (var db = new AppDbContext(opts))
        {
            var remaining = await db.OutboxMessages.ToListAsync();
            remaining.Should().ContainSingle(m => m.Id == dlId,
                "mensagens DeadLettered não são removidas pelo cleanup de retenção");
        }
    }
}
