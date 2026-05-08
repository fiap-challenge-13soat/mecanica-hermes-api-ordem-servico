using System.Diagnostics.CodeAnalysis;
using Mecanica.Hermes.Infrastructure.MassTransit.Observability;
using Mecanica.Hermes.Infrastructure.Persistence.Outbox;
using Mecanica.Hermes.Shared.Observability;
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

        // O nome do serviço segue a convenção do OTel: OTEL_SERVICE_NAME prevalece sobre o
        // valor padrão fixo. Isso mantém logs Serilog e traces/métricas OTel alinhados.
        var serviceName = OtlpEnvironment.ResolveServiceName("mecanica-hermes-ordem-servico-api");
        var serviceVersion = typeof(ObservabilityConfiguration).Assembly.GetName().Version?.ToString() ?? "unknown";

        services.AddOpenTelemetry()
            .UseOtlpExporter() // lê as env vars OTEL_EXPORTER_OTLP_*
            .ConfigureResource(resource =>
            {
                resource
                    .AddService(serviceName, serviceVersion: serviceVersion)
                    .AddContainerDetector() // injeta container.id a partir do cgroup
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
                    .AddSource(ActivitySources.ApiName)
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
                    .AddMeter(OutboxHealthMetrics.MeterName)
                    .AddMeter(DlqMetrics.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddNpgsqlInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            });
    }
}
