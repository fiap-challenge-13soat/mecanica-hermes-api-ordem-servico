using Mecanica.Hermes.Infrastructure.Observability;
using Mecanica.Hermes.Infrastructure.Settings;
using Serilog.Sinks.OpenTelemetry;

namespace Mecanica.Hermes.Infrastructure.Tests.Observability;

[Trait("Category", "Unit")]
public class OtlpEnvironmentTests : IDisposable
{
    private readonly List<string> _envVarsSet = [];

    public void Dispose()
    {
        foreach (var name in _envVarsSet)
            Environment.SetEnvironmentVariable(name, null);
        GC.SuppressFinalize(this);
    }

    private void SetEnv(string name, string value)
    {
        Environment.SetEnvironmentVariable(name, value);
        _envVarsSet.Add(name);
    }

    // -------------------------------------------------------------------------
    // ResolveLogsEndpoint
    // -------------------------------------------------------------------------

    [Fact]
    public void ResolveLogsEndpoint_SignalSpecificOverridesGeneric()
    {
        SetEnv(VariableNames.OtelExporterOtlpEndpoint, "https://generic.example.com:4318");
        SetEnv(VariableNames.OtelExporterOtlpLogsEndpoint, "https://logs.example.com:4318/v1/logs");

        OtlpEnvironment.ResolveLogsEndpoint().Should().Be("https://logs.example.com:4318/v1/logs");
    }

    [Fact]
    public void ResolveLogsEndpoint_HttpProtobuf_AppendsSignalPath()
    {
        SetEnv(VariableNames.OtelExporterOtlpEndpoint, "https://otlp.nr-data.net:4318");
        SetEnv(VariableNames.OtelExporterOtlpProtocol, "http/protobuf");

        OtlpEnvironment.ResolveLogsEndpoint().Should().Be("https://otlp.nr-data.net:4318/v1/logs");
    }

    [Fact]
    public void ResolveLogsEndpoint_Grpc_DoesNotAppendSignalPath()
    {
        SetEnv(VariableNames.OtelExporterOtlpEndpoint, "https://otlp.nr-data.net:4317");
        SetEnv(VariableNames.OtelExporterOtlpProtocol, "grpc");

        OtlpEnvironment.ResolveLogsEndpoint().Should().Be("https://otlp.nr-data.net:4317");
    }

    [Fact]
    public void ResolveLogsEndpoint_NoEnvVars_ReturnsDefaultUsEndpoint()
    {
        OtlpEnvironment.ResolveLogsEndpoint().Should().Be("https://otlp.nr-data.net:4318/v1/logs");
    }

    [Fact]
    public void ResolveLogsEndpoint_Grpc_TrailingSlash_IsTrimmed()
    {
        SetEnv(VariableNames.OtelExporterOtlpEndpoint, "https://otlp.nr-data.net:4317/");
        SetEnv(VariableNames.OtelExporterOtlpProtocol, "grpc");

        OtlpEnvironment.ResolveLogsEndpoint().Should().Be("https://otlp.nr-data.net:4317");
    }

    [Fact]
    public void ResolveLogsEndpoint_GenericEndpointWithTrailingSlash_NormalizesSlash()
    {
        SetEnv(VariableNames.OtelExporterOtlpEndpoint, "https://otlp.nr-data.net:4318/");
        SetEnv(VariableNames.OtelExporterOtlpProtocol, "http/protobuf");

        OtlpEnvironment.ResolveLogsEndpoint().Should().Be("https://otlp.nr-data.net:4318/v1/logs");
    }

    // -------------------------------------------------------------------------
    // ResolveLogsProtocol
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("http/protobuf", OtlpProtocol.HttpProtobuf)]
    [InlineData("grpc", OtlpProtocol.Grpc)]
    [InlineData("HTTP/PROTOBUF", OtlpProtocol.HttpProtobuf)]
    [InlineData("GRPC", OtlpProtocol.Grpc)]
    public void ResolveLogsProtocol_MapsKnownValues(string rawValue, OtlpProtocol expected)
    {
        SetEnv(VariableNames.OtelExporterOtlpProtocol, rawValue);

        OtlpEnvironment.ResolveLogsProtocol().Should().Be(expected);
    }

    [Fact]
    public void ResolveLogsProtocol_SignalSpecificOverridesGeneric()
    {
        SetEnv(VariableNames.OtelExporterOtlpProtocol, "grpc");
        SetEnv(VariableNames.OtelExporterOtlpLogsProtocol, "http/protobuf");

        OtlpEnvironment.ResolveLogsProtocol().Should().Be(OtlpProtocol.HttpProtobuf);
    }

    [Fact]
    public void ResolveLogsProtocol_UnknownValue_DefaultsToHttpProtobuf()
    {
        SetEnv(VariableNames.OtelExporterOtlpProtocol, "unknown-protocol");

        OtlpEnvironment.ResolveLogsProtocol().Should().Be(OtlpProtocol.HttpProtobuf);
    }

    [Fact]
    public void ResolveLogsProtocol_NoEnvVar_DefaultsToHttpProtobuf()
    {
        OtlpEnvironment.ResolveLogsProtocol().Should().Be(OtlpProtocol.HttpProtobuf);
    }

    // -------------------------------------------------------------------------
    // ResolveLogsHeaders
    // -------------------------------------------------------------------------

    [Fact]
    public void ResolveLogsHeaders_ParsesSingleHeader()
    {
        SetEnv(VariableNames.OtelExporterOtlpHeaders, "api-key=my-license-key");

        var headers = OtlpEnvironment.ResolveLogsHeaders();

        headers.Should().ContainKey("api-key").WhoseValue.Should().Be("my-license-key");
    }

    [Fact]
    public void ResolveLogsHeaders_ParsesMultipleHeaders()
    {
        SetEnv(VariableNames.OtelExporterOtlpHeaders, "api-key=my-key,x-custom=value");

        var headers = OtlpEnvironment.ResolveLogsHeaders();

        headers.Should().HaveCount(2);
        headers["api-key"].Should().Be("my-key");
        headers["x-custom"].Should().Be("value");
    }

    [Fact]
    public void ResolveLogsHeaders_SignalSpecificOverridesGeneric()
    {
        SetEnv(VariableNames.OtelExporterOtlpHeaders, "api-key=generic-key");
        SetEnv(VariableNames.OtelExporterOtlpLogsHeaders, "api-key=logs-key");

        var headers = OtlpEnvironment.ResolveLogsHeaders();

        headers["api-key"].Should().Be("logs-key");
    }

    [Fact]
    public void ResolveLogsHeaders_PercentDecodesValues()
    {
        SetEnv(VariableNames.OtelExporterOtlpHeaders, "api-key=hello%20world");

        var headers = OtlpEnvironment.ResolveLogsHeaders();

        headers["api-key"].Should().Be("hello world");
    }

    [Fact]
    public void ResolveLogsHeaders_MalformedPercentEncoding_ThrowsInvalidOperationException()
    {
        SetEnv(VariableNames.OtelExporterOtlpHeaders, "api-key=bad%GGvalue");

        var act = OtlpEnvironment.ResolveLogsHeaders;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*api-key*")
            .WithMessage("*OTEL_EXPORTER_OTLP_HEADERS*");
    }

    [Fact]
    public void ResolveLogsHeaders_MissingEqualsSeparator_ThrowsInvalidOperationException()
    {
        SetEnv(VariableNames.OtelExporterOtlpHeaders, "api-key=valid,malformed-pair");

        var act = OtlpEnvironment.ResolveLogsHeaders;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*malformed-pair*")
            .WithMessage("*OTEL_EXPORTER_OTLP_HEADERS*");
    }

    [Fact]
    public void ResolveLogsHeaders_EmptyKey_ThrowsInvalidOperationException()
    {
        SetEnv(VariableNames.OtelExporterOtlpHeaders, "=value");

        var act = OtlpEnvironment.ResolveLogsHeaders;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty header name*");
    }

    [Fact]
    public void ResolveLogsHeaders_NoEnvVar_ReturnsEmptyDictionary()
    {
        OtlpEnvironment.ResolveLogsHeaders().Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // ResolveServiceName
    // -------------------------------------------------------------------------

    [Fact]
    public void ResolveServiceName_EnvVarSet_ReturnsEnvValue()
    {
        SetEnv(VariableNames.OtelServiceName, "my-custom-service");

        OtlpEnvironment.ResolveServiceName("default-name").Should().Be("my-custom-service");
    }

    [Fact]
    public void ResolveServiceName_NoEnvVar_ReturnsDefaultName()
    {
        OtlpEnvironment.ResolveServiceName("default-name").Should().Be("default-name");
    }
}
