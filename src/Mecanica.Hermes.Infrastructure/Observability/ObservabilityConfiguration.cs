using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Mecanica.Hermes.Infrastructure.Observability;

[ExcludeFromCodeCoverage(Justification =
    "Composition root de OpenTelemetry — configuração testada em integração e em runtime.")]
internal static class ObservabilityConfiguration
{
    public static void AddObservability(this IServiceCollection services)
    {
        if (!EnvironmentDetector.IsObservabilityEnabled)
            return;

        // Service name follows the OTel convention: OTEL_SERVICE_NAME wins over the
        // hard-coded default. This keeps Serilog logs and OTel traces/metrics aligned.
        var serviceName = OtlpEnvironment.ResolveServiceName("mecanica-hermes-ordem-servico-api");
        var serviceVersion = typeof(ObservabilityConfiguration).Assembly.GetName().Version?.ToString() ?? "unknown";

        services.AddOpenTelemetry()
            .UseOtlpExporter() // reads OTEL_EXPORTER_OTLP_* env vars
            .ConfigureResource(resource =>
            {
                resource
                    .AddService(serviceName, serviceVersion: serviceVersion)
                    .AddContainerDetector() // injects container.id from cgroup
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["k8s.pod.name"] = EnvironmentDetector.PodName,
                        ["k8s.namespace.name"] = EnvironmentDetector.PodNamespace,
                        ["k8s.node.name"] = EnvironmentDetector.NodeName,
                        ["k8s.cluster.name"] = EnvironmentDetector.ClusterName,
                        ["k8s.deployment.name"] = "mecanica-hermes-api",
                        ["host.name"] = Environment.MachineName
                    });
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ActivitySources.ApplicationName)
                    .AddSource("MecanicaHermes.Application")
                    .AddNpgsql()
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                        opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health") &&
                                             !ctx.Request.Path.StartsWithSegments("/healthz");
                    })
                    .AddHttpClientInstrumentation();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(ActivitySources.ApplicationName)
                    .AddMeter(OrdemDeServicoMetrics.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddNpgsqlInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            });
    }
}
