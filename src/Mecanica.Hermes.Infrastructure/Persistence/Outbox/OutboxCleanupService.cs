using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CA1873 // Log arguments are simple property/variable accesses — not expensive
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mecanica.Hermes.Infrastructure.Persistence.Outbox;

/// <summary>
/// Serviço de limpeza (retention) da tabela <c>OutboxMessages</c>.
///
/// Remove periodicamente mensagens com <see cref="OutboxMessageStatus.Processed"/> cuja
/// data de processamento é anterior a <see cref="OutboxOptions.ProcessedRetention"/> (default: 7 dias).
/// Roda com período 10× o <see cref="OutboxOptions.PollInterval"/>, portanto não compete
/// com o <see cref="OutboxProcessor"/> em frequência de I/O.
/// </summary>
internal sealed class OutboxCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxCleanupService> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cleanupInterval = _options.PollInterval * 10;
        logger.LogInformation("OutboxCleanupService iniciado. Cleanup a cada {Interval}.", cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(cleanupInterval, stoppingToken);
                await CleanupAsync(stoppingToken);
            }
            catch (TaskCanceledException) { /* shutdown */ }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Falha ao executar cleanup do Outbox.");
            }
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow - _options.ProcessedRetention;

        var deleted = await db.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Processed && m.ProcessedOn <= cutoff)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            logger.LogInformation("OutboxCleanupService removeu {Count} mensagens processadas anteriores a {Cutoff:O}.", deleted, cutoff);
    }
}
