using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Serilog.Events;

namespace Mecanica.Hermes.Infrastructure.Logging;

[ExcludeFromCodeCoverage(
    Justification = "Enricher de Serilog exercitado em produção; teste direto não agrega valor.")]
public class LogLevelEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var level = logEvent.Level switch
        {
            LogEventLevel.Verbose => "TRACE",
            LogEventLevel.Debug => "DEBUG",
            LogEventLevel.Information => "INFO",
            LogEventLevel.Warning => "WARN",
            LogEventLevel.Error => "ERROR",
            LogEventLevel.Fatal => "FATAL",
            _ => logEvent.Level.ToString().ToUpperInvariant()
        };

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("level", level));
    }
}
