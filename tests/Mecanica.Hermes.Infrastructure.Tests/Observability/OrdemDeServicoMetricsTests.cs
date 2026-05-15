using System.Diagnostics.Metrics;
using Mecanica.Hermes.Infrastructure.Observability;
using Microsoft.Extensions.DependencyInjection;

namespace Mecanica.Hermes.Infrastructure.Tests.Observability;

/// <summary>
///     Testa que <see cref="OrdemDeServicoMetrics" /> incrementa os contadores/histograms
///     corretamente. Usa <see cref="MeterListener" /> para capturar as medidas sem depender
///     de um exportador OTLP real.
/// </summary>
[Trait("Category", "Unit")]
public class OrdemDeServicoMetricsTests : IDisposable
{
    private readonly List<(string name, object value, KeyValuePair<string, object?>[] tags)> _captured = [];
    private readonly MeterListener _listener;
    private readonly OrdemDeServicoMetrics _metrics;

    public OrdemDeServicoMetricsTests()
    {
        var factory = new ServiceCollection().AddMetrics().BuildServiceProvider()
            .GetRequiredService<IMeterFactory>();
        _metrics = new OrdemDeServicoMetrics(factory);

        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == OrdemDeServicoMetrics.MeterName)
                    listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            _captured.Add((instrument.Name, measurement, tags.ToArray())));
        _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
            _captured.Add((instrument.Name, measurement, tags.ToArray())));
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _metrics.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void OrdemCriada_DeveIncrementarContador()
    {
        _metrics.OrdemCriada();

        _captured.Should().ContainSingle(c => c.name == "ordemdeservico.criadas" && (long)c.value == 1L);
    }

    [Fact]
    public void EtapaAvancada_DeveIncrementarContadorComTags()
    {
        _metrics.EtapaAvancada("Recebida", "EmDiagnostico");

        var measurement = _captured.Should().ContainSingle(c => c.name == "ordemdeservico.etapa_avancada").Subject;
        measurement.tags.Should().Contain(new KeyValuePair<string, object?>("status_anterior", "Recebida"));
        measurement.tags.Should().Contain(new KeyValuePair<string, object?>("status_novo", "EmDiagnostico"));
        measurement.tags.Should().Contain(new KeyValuePair<string, object?>("tipo_movimentacao", "etapa_avancada"));
    }

    [Fact]
    public void EtapaRevertida_DeveIncrementarContadorComTagsEspecificas()
    {
        _metrics.EtapaRevertida("EmDiagnostico", "Recebida");

        var measurement = _captured.Should().ContainSingle(c => c.name == "ordemdeservico.etapa_avancada").Subject;
        measurement.tags.Should().Contain(new KeyValuePair<string, object?>("tipo_movimentacao", "etapa_revertida"));
    }

    [Fact]
    public void ErroRegistrado_DeveIncrementarContador()
    {
        _metrics.ErroRegistrado("avancar", "nao_encontrada");

        var measurement = _captured.Should().ContainSingle(c => c.name == "ordemdeservico.erros").Subject;
        measurement.tags.Should().Contain(new KeyValuePair<string, object?>("operacao", "avancar"));
        measurement.tags.Should().Contain(new KeyValuePair<string, object?>("tipo_erro", "nao_encontrada"));
    }

    [Fact]
    public void OrdemCancelada_DeveIncrementarContador()
    {
        _metrics.OrdemCancelada("Recebida");

        var measurement = _captured.Should().ContainSingle(c => c.name == "ordemdeservico.canceladas").Subject;
        measurement.tags.Should().Contain(new KeyValuePair<string, object?>("status_anterior", "Recebida"));
    }

    [Fact]
    public void DuracaoEtapaRegistrada_DeveRegistrarNoHistograma()
    {
        _metrics.DuracaoEtapaRegistrada("Recebida", 3.5);

        _captured.Should()
            .ContainSingle(c => c.name == "ordemdeservico.duracao_etapa_minutes" && (double)c.value == 3.5);
    }

    [Fact]
    public void DistribuicaoAtualizada_SemStatusAnterior_DeveIncrementarStatusNovo()
    {
        _metrics.DistribuicaoAtualizada("Recebida", null);

        var m = _captured.Should().ContainSingle(c => c.name == "ordemdeservico.por_status").Subject;
        ((long)m.value).Should().Be(1);
        m.tags.Should().Contain(new KeyValuePair<string, object?>("status", "Recebida"));
    }

    [Fact]
    public void DistribuicaoAtualizada_ComStatusAnterior_DeveIncrementarNovoEDecrementarAnterior()
    {
        _metrics.DistribuicaoAtualizada("EmDiagnostico", "Recebida");

        _captured.Should().HaveCount(2);
        _captured.Should().Contain(c =>
            c.name == "ordemdeservico.por_status" && (long)c.value == 1 &&
            c.tags.Any(t => t.Key == "status" && (string?)t.Value == "EmDiagnostico"));
        _captured.Should().Contain(c =>
            c.name == "ordemdeservico.por_status" && (long)c.value == -1 &&
            c.tags.Any(t => t.Key == "status" && (string?)t.Value == "Recebida"));
    }
}
