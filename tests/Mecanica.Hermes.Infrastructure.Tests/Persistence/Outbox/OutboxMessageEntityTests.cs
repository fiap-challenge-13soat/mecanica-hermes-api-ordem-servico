using Mecanica.Hermes.Infrastructure.Persistence.Outbox;

namespace Mecanica.Hermes.Infrastructure.Tests.Persistence.Outbox;

[Trait("Category", "Unit")]
public class OutboxMessageEntityTests
{
    [Fact]
    public void Construtor_DevePopularPropriedadesEIniciarComoPending()
    {
        var id = Guid.NewGuid();
        var occurredOn = DateTime.UtcNow;

        var entity = new OutboxMessageEntity(id, "EventType", "{\"foo\":1}", occurredOn, "trace-123");

        entity.Id.Should().Be(id);
        entity.Type.Should().Be("EventType");
        entity.Payload.Should().Be("{\"foo\":1}");
        entity.OccurredOn.Should().Be(occurredOn);
        entity.TraceContext.Should().Be("trace-123");
        entity.Status.Should().Be(OutboxMessageStatus.Pending);
        entity.RetryCount.Should().Be(0);
        entity.LastError.Should().BeNull();
        entity.ProcessedOn.Should().BeNull();
    }

    [Fact]
    public void Construtor_QuandoTraceContextOmitido_DeveSerNulo()
    {
        var entity = new OutboxMessageEntity(Guid.NewGuid(), "T", "{}", DateTime.UtcNow);

        entity.TraceContext.Should().BeNull();
    }

    [Fact]
    public void MarkAsProcessed_DeveTransicionarParaProcessedELimparErro()
    {
        var entity = new OutboxMessageEntity(Guid.NewGuid(), "T", "{}", DateTime.UtcNow);
        entity.MarkAsFailed("erro anterior");

        var processedAt = DateTime.UtcNow;
        entity.MarkAsProcessed(processedAt);

        entity.Status.Should().Be(OutboxMessageStatus.Processed);
        entity.ProcessedOn.Should().Be(processedAt);
        entity.LastError.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_DeveIncrementarRetryCountERegistrarErro()
    {
        var entity = new OutboxMessageEntity(Guid.NewGuid(), "T", "{}", DateTime.UtcNow);

        entity.MarkAsFailed("falha 1");

        entity.RetryCount.Should().Be(1);
        entity.LastError.Should().Be("falha 1");
        entity.Status.Should().Be(OutboxMessageStatus.Pending);
    }

    [Fact]
    public void MarkAsFailed_QuandoAtingirMaxRetriesDefault_DeveIrParaDeadLettered()
    {
        var entity = new OutboxMessageEntity(Guid.NewGuid(), "T", "{}", DateTime.UtcNow);

        for (var i = 0; i < OutboxMessageEntity.DefaultMaxRetries; i++)
            entity.MarkAsFailed($"falha {i + 1}");

        entity.Status.Should().Be(OutboxMessageStatus.DeadLettered);
        entity.RetryCount.Should().Be(OutboxMessageEntity.DefaultMaxRetries);
        entity.LastError.Should().Be($"falha {OutboxMessageEntity.DefaultMaxRetries}");
    }

    [Fact]
    public void MarkAsFailed_ComMaxRetriesCustomizado_DeveRespeitarOLimite()
    {
        var entity = new OutboxMessageEntity(Guid.NewGuid(), "T", "{}", DateTime.UtcNow);

        entity.MarkAsFailed("falha 1", maxRetries: 2);
        entity.Status.Should().Be(OutboxMessageStatus.Pending);

        entity.MarkAsFailed("falha 2", maxRetries: 2);
        entity.Status.Should().Be(OutboxMessageStatus.DeadLettered);
        entity.RetryCount.Should().Be(2);
    }
}
