using System.Diagnostics.Metrics;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;

namespace Mecanica.Hermes.Infrastructure.Observability;

internal sealed class OrdemDeServicoMetrics : IOrdemDeServicoMetrics, IDisposable
{
    public const string MeterName = "MecanicaHermes.Business";
    private readonly Histogram<double> _duracaoEtapa;
    private readonly Counter<long> _erros;
    private readonly Counter<long> _etapaAvancada;

    private readonly Meter _meter;
    private readonly Counter<long> _ordemCanceladas;
    private readonly Counter<long> _ordemCriadas;

    public OrdemDeServicoMetrics()
    {
        _meter = new Meter(MeterName);
        _ordemCriadas = _meter.CreateCounter<long>(
            "ordemdeservico.criadas",
            description: "Número de ordens de serviço criadas");
        _etapaAvancada = _meter.CreateCounter<long>(
            "ordemdeservico.etapa_avancada",
            description: "Número de avanços de etapa em ordens de serviço");
        _erros = _meter.CreateCounter<long>(
            "ordemdeservico.erros",
            description: "Número de erros em operações de ordem de serviço");
        _duracaoEtapa = _meter.CreateHistogram<double>(
            "ordemdeservico.duracao_etapa_minutes",
            "minutes",
            "Tempo gasto em cada etapa da ordem de serviço");
        _ordemCanceladas = _meter.CreateCounter<long>(
            "ordemdeservico.canceladas",
            description: "Número de ordens de serviço canceladas");
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public void OrdemCriada()
    {
        _ordemCriadas.Add(1);
    }

    public void EtapaAvancada(string statusAnterior, string statusNovo)
    {
        _etapaAvancada.Add(1,
            new KeyValuePair<string, object?>("status_anterior", statusAnterior),
            new KeyValuePair<string, object?>("status_novo", statusNovo),
            new KeyValuePair<string, object?>("tipo_movimentacao", "etapa_avancada"));
    }

    public void EtapaRevertida(string statusAnterior, string statusNovo)
    {
        _etapaAvancada.Add(1,
            new KeyValuePair<string, object?>("status_anterior", statusAnterior),
            new KeyValuePair<string, object?>("status_novo", statusNovo),
            new KeyValuePair<string, object?>("tipo_movimentacao", "etapa_revertida"));
    }

    public void ErroRegistrado(string operacao, string tipoErro)
    {
        _erros.Add(1,
            new KeyValuePair<string, object?>("operacao", operacao),
            new KeyValuePair<string, object?>("tipo_erro", tipoErro));
    }

    public void DuracaoEtapaRegistrada(string status, double duracaoMinutes)
    {
        _duracaoEtapa.Record(duracaoMinutes,
            new KeyValuePair<string, object?>("status", status));
    }

    public void OrdemCancelada(string statusAnterior)
    {
        _ordemCanceladas.Add(1,
            new KeyValuePair<string, object?>("status_anterior", statusAnterior));
    }
}
