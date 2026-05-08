using System.Diagnostics;
using System.Text.Json;
using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Infrastructure.Observability;
using Mecanica.Hermes.Shared.Core.Domain;
using Mecanica.Hermes.Shared.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mecanica.Hermes.Infrastructure.Persistence.Outbox;

/// <summary>
///     Background service que processa mensagens pendentes da tabela <c>OutboxMessages</c>.
///     <list type="bullet">
///         <item>
///             <b>Concorrência:</b> usa <c>FOR UPDATE SKIP LOCKED</c> (PostgreSQL) para que múltiplas
///             réplicas do serviço não processem o mesmo lote simultaneamente.
///         </item>
///         <item>
///             <b>DLQ:</b> após <see cref="OutboxOptions.MaxRetries" /> falhas consecutivas, a mensagem
///             é marcada como <see cref="OutboxMessageStatus.DeadLettered" /> e não reprocessada.
///         </item>
///         <item>
///             <b>Tracing:</b> cada ciclo abre um <see cref="Activity" /> filho propagando o
///             <c>TraceContext</c> original do evento.
///         </item>
///         <item>
///             <b>Métricas:</b> emite <c>outbox.dispatched</c>, <c>outbox.failed</c> e
///             <c>outbox.dead_lettered</c> via <see cref="OutboxHealthMetrics" />.
///         </item>
///     </list>
/// </summary>
internal sealed partial class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    OutboxHealthMetrics metrics,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogIniciado(logger, _options.PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                LogFalhaLote(logger, ex, _options.PollInterval);
            }

            try
            {
                await Task.Delay(_options.PollInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                /* shutdown */
            }
        }

        LogParado(logger);
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        // SELECT … FOR UPDATE SKIP LOCKED garante que instâncias concorrentes não peguem o
        // mesmo lote. O SKIP LOCKED é suportado nativamente no PostgreSQL 9.5+.
        // Em InMemory (testes) o FromSqlRaw não é suportado — retorna 0 e o path de fallback
        // é exercitado nos testes unitários.
        List<OutboxMessageEntity> pending;
        try
        {
            pending = await db.OutboxMessages
                .FromSqlRaw(
                    """
                    SELECT * FROM "OutboxMessages"
                    WHERE "Status" = 0
                    ORDER BY "OccurredOn"
                    LIMIT {0}
                    FOR UPDATE SKIP LOCKED
                    """,
                    _options.BatchSize)
                .ToListAsync(ct);
        }
        catch (InvalidOperationException)
        {
            // Fallback para providers que não suportam FromSqlRaw (ex: InMemory em testes).
            // ATENÇÃO: sem FOR UPDATE SKIP LOCKED, múltiplas instâncias podem processar o mesmo lote.
            // Use OUTBOX__ALLOW_NON_LOCKING_FALLBACK=true apenas em ambientes de teste.
            if (!_options.AllowNonLockingFallback)
                LogFallbackSemLock(logger);

            pending = await db.OutboxMessages
                .Where(m => m.Status == OutboxMessageStatus.Pending)
                .OrderBy(m => m.OccurredOn)
                .Take(_options.BatchSize)
                .ToListAsync(ct);
        }

        if (pending.Count == 0) return;

        foreach (var message in pending)
        {
            using var activity = ActivitySources.Api.StartActivity(
                $"outbox.dispatch/{message.Type}",
                ActivityKind.Internal,
                message.TraceContext ?? string.Empty);

            activity?.SetTag("outbox.message_id", message.Id.ToString());
            activity?.SetTag("outbox.event_type", message.Type);
            activity?.SetTag("outbox.retry_count", message.RetryCount);

            try
            {
                var evtType = ResolveEventType(message.Type);
                if (evtType is null)
                {
                    var err = $"Tipo '{message.Type}' não pôde ser resolvido.";
                    LogMensagemInvalida(logger, message.Id, err);
                    message.MarkAsFailed(err, _options.MaxRetries);
                    RecordFailure(message, activity);
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Payload, evtType) as IDomainEvent;
                if (domainEvent is null)
                {
                    const string err = "Payload deserializado é null ou não implementa IDomainEvent.";
                    LogMensagemInvalida(logger, message.Id, err);
                    message.MarkAsFailed(err, _options.MaxRetries);
                    RecordFailure(message, activity);
                    continue;
                }

                await dispatcher.DispatchAsync([domainEvent], ct);
                message.MarkAsProcessed(_timeProvider.GetUtcNow().UtcDateTime);
                metrics.MessageDispatched();
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                LogFalhaDispatch(logger, ex, message.Id, message.Type, message.RetryCount + 1);
                message.MarkAsFailed(ex.Message, _options.MaxRetries);
                RecordFailure(message, activity);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private void RecordFailure(OutboxMessageEntity message, Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Error, message.LastError);
        if (message.Status == OutboxMessageStatus.DeadLettered)
        {
            metrics.MessageDeadLettered();
            LogDeadLetter(logger, message.Id, message.Type, _options.MaxRetries);
        }
        else
        {
            metrics.MessageFailed();
        }
    }

    /// <summary>
    ///     Resolve o <see cref="Type" /> concreto a partir do discriminador persistido.
    ///     Suporta tanto nomes estáveis (<c>DomainEventNameAttribute</c>) quanto
    ///     <c>AssemblyQualifiedName</c> legados.
    /// </summary>
    private static Type? ResolveEventType(string typeName)
    {
        // Tentativa 1: AssemblyQualifiedName ou FullName (legado)
        var resolved = Type.GetType(typeName, false);
        if (resolved is not null) return resolved;

        // Tentativa 2: varredura por DomainEventNameAttribute no assembly do domínio
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch
                {
                    return [];
                }
            })
            .FirstOrDefault(t =>
            {
                var attr = t.GetCustomAttributes(typeof(DomainEventNameAttribute), false)
                    .OfType<DomainEventNameAttribute>()
                    .FirstOrDefault();
                return attr?.Name == typeName;
            });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "OutboxProcessor iniciado. Polling a cada {Interval}.")]
    private static partial void LogIniciado(ILogger logger, TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Information, Message = "OutboxProcessor parado.")]
    private static partial void LogParado(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Falha ao processar lote do Outbox. Próxima tentativa em {Interval}.")]
    private static partial void LogFalhaLote(ILogger logger, Exception exception, TimeSpan interval);

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "OutboxProcessor: o provider de banco não suporta FOR UPDATE SKIP LOCKED. "
                  + "O fallback LINQ está sendo usado sem AllowNonLockingFallback=true. "
                  + "Em múltiplas instâncias, isso pode causar double-delivery de eventos. "
                  + "Defina OUTBOX__ALLOW_NON_LOCKING_FALLBACK=true para suprimir este aviso em testes.")]
    private static partial void LogFallbackSemLock(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "OutboxMessage {Id}: {Error}")]
    private static partial void LogMensagemInvalida(ILogger logger, Guid id, string error);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Falha ao despachar OutboxMessage {Id} ({Type}). Retry={Retry}")]
    private static partial void LogFalhaDispatch(
        ILogger logger,
        Exception exception,
        Guid id,
        string type,
        int retry);

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "OutboxMessage {Id} ({Type}) atingiu o limite de {MaxRetries} tentativas e foi movida "
                  + "para Dead Letter.")]
    private static partial void LogDeadLetter(ILogger logger, Guid id, string type, int maxRetries);
}
