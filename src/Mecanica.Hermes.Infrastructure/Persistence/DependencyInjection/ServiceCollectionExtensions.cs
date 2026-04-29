using System.Diagnostics;
using System.Text.RegularExpressions;
using Mecanica.Hermes.Application.Common.Persistence;
using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Infrastructure.Persistence.Events;
using Mecanica.Hermes.Infrastructure.Persistence.Outbox;
using Mecanica.Hermes.Infrastructure.Persistence.UnitOfWork;
using Mecanica.Hermes.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Mecanica.Hermes.Infrastructure.Persistence.DependencyInjection;

internal static partial class ServiceCollectionExtensions
{
    public static void AddEntityFrameworkConfiguration(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddTransient<IUnitOfWork, EfUnitOfWork>();
        services.AddTransient<IDomainEventDispatcher, DomainEventDispatcher>();

        services.Configure<OutboxOptions>(opts =>
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("OUTBOX__BATCH_SIZE"), out var batch) && batch > 0)
                opts.BatchSize = batch;
            if (int.TryParse(Environment.GetEnvironmentVariable("OUTBOX__MAX_RETRIES"), out var retries) && retries > 0)
                opts.MaxRetries = retries;
            if (int.TryParse(Environment.GetEnvironmentVariable("OUTBOX__POLL_INTERVAL_SECONDS"), out var poll) && poll > 0)
                opts.PollInterval = TimeSpan.FromSeconds(poll);
            if (int.TryParse(Environment.GetEnvironmentVariable("OUTBOX__RETENTION_DAYS"), out var days) && days > 0)
                opts.ProcessedRetention = TimeSpan.FromDays(days);
            if (bool.TryParse(Environment.GetEnvironmentVariable("OUTBOX__ALLOW_NON_LOCKING_FALLBACK"), out var fallback))
                opts.AllowNonLockingFallback = fallback;
        });

        services.AddSingleton<OutboxHealthMetrics>();
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<OutboxCleanupService>();

        var connectionString = Environment.GetEnvironmentVariable(VariableNames.DefaultConnectionString);

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.ConfigureTracing(tracing => tracing
            .ConfigureCommandSpanNameProvider(cmd => BuildSpanName(cmd.CommandText))
            .ConfigureCommandEnrichmentCallback((activity, cmd) =>
            {
                activity.SetTag("db.statement", cmd.CommandText);

                var match = SqlOperationRegex().Match(cmd.CommandText);
                if (!match.Success) return;

                var op = match.Groups["op"].Value.Split(' ')[0].ToUpperInvariant();
                var table = match.Groups["table"].Value
                    .Replace("\"", "").Replace("public.", "").Trim();

                // Required by New Relic APM Databases view (OTel semconv v1.33)
                activity.SetTag("db.operation.name", op);
                activity.SetTag("db.collection.name", table);
            }));

        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);

        services.AddDbContext<AppDbContext>(opt =>
        {
            opt.UseNpgsql(dataSource,
                b => b.MigrationsAssembly(typeof(IInfrastructureAssemblyMarker).Namespace)
                    .MigrationsHistoryTable("__EFMigrationsHistory", "public"));
        });
    }

    private static string BuildSpanName(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return "Postgres";

        var match = SqlOperationRegex().Match(sql);
        if (!match.Success)
            return "Postgres";

        var operation = match.Groups["op"].Value.ToLowerInvariant();
        var table = match.Groups["table"].Value.ToLowerInvariant()
            .Replace("\"", "").Replace("public.", "");

        return $"{table} {operation}";
    }

    [GeneratedRegex(
        @"^\s*(?<op>SELECT|INSERT\s+INTO|UPDATE|DELETE\s+FROM)\s+(?<table>""?[\w.]+""\s*\.?\s*""?[\w.]+""?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SqlOperationRegex();
}
