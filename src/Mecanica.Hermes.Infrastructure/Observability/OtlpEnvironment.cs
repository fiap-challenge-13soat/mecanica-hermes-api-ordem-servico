using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Mecanica.Hermes.Infrastructure.Settings;
using Serilog.Sinks.OpenTelemetry;

namespace Mecanica.Hermes.Infrastructure.Observability;

/// <summary>
///     Resolve a configuração do exporter OTLP seguindo o contrato de variáveis de ambiente
///     da especificação OpenTelemetry. Veja
///     <see href="https://opentelemetry.io/docs/specs/otel/protocol/exporter/">OTLP Exporter Configuration</see>
///     e
///     <see href="https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/">
///         SDK Environment
///         Variables
///     </see>
///     .
///     Precedência (maior primeiro):
///     <list type="number">
///         <item>Env var específica do sinal (ex.: <c>OTEL_EXPORTER_OTLP_LOGS_*</c>).</item>
///         <item>Env var genérica (ex.: <c>OTEL_EXPORTER_OTLP_*</c>).</item>
///         <item>Valor padrão fixo (apenas quando há um default seguro e não ambíguo).</item>
///     </list>
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Resolução de variáveis de ambiente OTLP — composição de configuração observada em runtime.")]
internal static class OtlpEnvironment
{
    private const string DefaultLogsEndpoint = "https://otlp.nr-data.net:4318/v1/logs";
    private const string LogsSignalPath = "/v1/logs";

    /// <summary>
    ///     Resolve a URL do endpoint OTLP de logs.
    ///     A variável específica do sinal é usada como está; à variável genérica é acrescentado
    ///     <c>/v1/logs</c> apenas para <c>http/protobuf</c> (conforme spec OTel).
    ///     Para <c>grpc</c>, o endpoint é usado como está, sem path de sinal.
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
    ///     Resolve os headers do exporter OTLP para logs.
    ///     A variável específica do sinal <c>OTEL_EXPORTER_OTLP_LOGS_HEADERS</c> é
    ///     consultada primeiro, com fallback para a genérica
    ///     <c>OTEL_EXPORTER_OTLP_HEADERS</c> quando não há headers específicos para logs.
    /// </summary>
    public static IDictionary<string, string> ResolveLogsHeaders()
    {
        var raw = Environment.GetEnvironmentVariable(VariableNames.OtelExporterOtlpLogsHeaders)
                  ?? Environment.GetEnvironmentVariable(VariableNames.OtelExporterOtlpHeaders);

        return ParseHeaders(raw);
    }

    /// <summary>
    ///     Resolve o protocolo de transporte OTLP para logs.
    ///     O padrão é <see cref="OtlpProtocol.HttpProtobuf" />, pois é o único protocolo
    ///     aceito pelo endpoint OTLP HTTP do New Relic na porta 4318.
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
    ///     Resolve o nome do serviço a partir de <c>OTEL_SERVICE_NAME</c>, com fallback
    ///     para <paramref name="defaultName" /> quando a env var não está definida.
    ///     Espelha o comportamento de auto-detecção do OpenTelemetry .NET SDK
    ///     para que traces, métricas e logs compartilhem o mesmo <c>service.name</c>.
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
    ///     Faz o parse de uma string de headers OTLP no formato <c>key1=value1,key2=value2</c>,
    ///     decodificando cada valor por percent-encoding conforme exigido pela especificação OTel.
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
