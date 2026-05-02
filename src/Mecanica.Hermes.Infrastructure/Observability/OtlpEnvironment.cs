using System.Text.RegularExpressions;
using Mecanica.Hermes.Infrastructure.Settings;
using Serilog.Sinks.OpenTelemetry;

namespace Mecanica.Hermes.Infrastructure.Observability;

/// <summary>
///     Resolves the OTLP exporter configuration following the OpenTelemetry
///     specification's environment variable contract. See
///     <see href="https://opentelemetry.io/docs/specs/otel/protocol/exporter/">OTLP Exporter Configuration</see>
///     and
///     <see href="https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/">
///         SDK Environment
///         Variables
///     </see>
///     .
///     Precedence (highest first):
///     <list type="number">
///         <item>Signal-specific env var (e.g. <c>OTEL_EXPORTER_OTLP_LOGS_*</c>).</item>
///         <item>Generic env var (e.g. <c>OTEL_EXPORTER_OTLP_*</c>).</item>
///         <item>Hard-coded default (only when an unambiguous safe default exists).</item>
///     </list>
/// </summary>
internal static class OtlpEnvironment
{
    private const string DefaultLogsEndpoint = "https://otlp.nr-data.net:4318/v1/logs";
    private const string LogsSignalPath = "/v1/logs";

    /// <summary>
    ///     Resolves the OTLP logs endpoint URL.
    ///     The signal-specific variable is used as-is; the generic variable has
    ///     <c>/v1/logs</c> appended only for <c>http/protobuf</c> (per OTel spec).
    ///     For <c>grpc</c>, the endpoint is used as-is without a signal path.
    /// </summary>
    public static string ResolveLogsEndpoint()
    {
        var signalSpecific = Environment.GetEnvironmentVariable(VariableNames.OtelExporterOtlpLogsEndpoint);
        if (!string.IsNullOrWhiteSpace(signalSpecific))
            return signalSpecific.Trim();

        var generic = Environment.GetEnvironmentVariable(VariableNames.OtelExporterOtlpEndpoint);
        if (!string.IsNullOrWhiteSpace(generic))
        {
            var protocol = ResolveLogsProtocol();
            return protocol == OtlpProtocol.Grpc
                ? generic.Trim().TrimEnd('/')
                : $"{generic.Trim().TrimEnd('/')}{LogsSignalPath}";
        }

        return DefaultLogsEndpoint;
    }

    /// <summary>
    ///     Resolves the OTLP exporter headers for logs.
    ///     The signal-specific variable <c>OTEL_EXPORTER_OTLP_LOGS_HEADERS</c> is
    ///     consulted first, falling back to the generic
    ///     <c>OTEL_EXPORTER_OTLP_HEADERS</c> when no logs-specific headers are set.
    /// </summary>
    public static IDictionary<string, string> ResolveLogsHeaders()
    {
        var raw = Environment.GetEnvironmentVariable(VariableNames.OtelExporterOtlpLogsHeaders)
                  ?? Environment.GetEnvironmentVariable(VariableNames.OtelExporterOtlpHeaders);

        return ParseHeaders(raw);
    }

    /// <summary>
    ///     Resolves the OTLP transport protocol for logs.
    ///     Defaults to <see cref="OtlpProtocol.HttpProtobuf" /> because that is the only
    ///     protocol the New Relic OTLP HTTP endpoint accepts on port 4318.
    /// </summary>
    public static OtlpProtocol ResolveLogsProtocol()
    {
        var raw = Environment.GetEnvironmentVariable(VariableNames.OtelExporterOtlpLogsProtocol)
                  ?? Environment.GetEnvironmentVariable(VariableNames.OtelExporterOtlpProtocol);

        return raw?.Trim().ToLowerInvariant() switch
        {
            "http/protobuf" => OtlpProtocol.HttpProtobuf,
            "grpc" => OtlpProtocol.Grpc,
            _ => OtlpProtocol.HttpProtobuf
        };
    }

    /// <summary>
    ///     Resolves the service name from <c>OTEL_SERVICE_NAME</c>, falling back
    ///     to <paramref name="defaultName" /> when the env var is unset.
    ///     This mirrors the auto-detection behaviour of the OpenTelemetry .NET SDK
    ///     so that traces, metrics and logs share the same <c>service.name</c>.
    /// </summary>
    public static string ResolveServiceName(string defaultName)
    {
        var fromEnv = Environment.GetEnvironmentVariable(VariableNames.OtelServiceName);
        return string.IsNullOrWhiteSpace(fromEnv) ? defaultName : fromEnv.Trim();
    }

    private static bool IsValidPercentEncoding(string value)
    {
        return !Regex.IsMatch(value, @"%(?![0-9A-Fa-f]{2})",
            RegexOptions.None, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    ///     Parses an OTLP headers string formatted as <c>key1=value1,key2=value2</c>,
    ///     percent-decoding each value as required by the OTel specification.
    /// </summary>
    private static Dictionary<string, string> ParseHeaders(string? raw)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        foreach (var pair in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = pair.IndexOf('=');
            if (separatorIndex < 0)
                throw new InvalidOperationException(
                    $"The OTLP header pair '{pair}' is missing a '=' separator. " +
                    "Each entry must be formatted as 'key=value'. " +
                    "Check the OTEL_EXPORTER_OTLP_HEADERS or OTEL_EXPORTER_OTLP_LOGS_HEADERS environment variable.");

            var key = pair[..separatorIndex].Trim();
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException(
                    $"The OTLP header pair '{pair}' has an empty header name. " +
                    "Each entry must be formatted as 'key=value' with a non-empty header name. " +
                    "Check the OTEL_EXPORTER_OTLP_HEADERS or OTEL_EXPORTER_OTLP_LOGS_HEADERS environment variable.");

            var rawValue = pair[(separatorIndex + 1)..].Trim();
            if (!IsValidPercentEncoding(rawValue))
                throw new InvalidOperationException(
                    $"The OTLP header value for key '{key}' contains malformed percent-encoding: '{rawValue}'. " +
                    "Check the OTEL_EXPORTER_OTLP_HEADERS or OTEL_EXPORTER_OTLP_LOGS_HEADERS environment variable.");
            var value = Uri.UnescapeDataString(rawValue);
            result[key] = value;
        }

        return result;
    }
}
