namespace Mecanica.Hermes.Application.OrdensDeServico.Ports;

public interface IOrdemDeServicoMetrics
{
    void OrdemCriada();
    void EtapaAvancada(string statusAnterior, string statusNovo);
    void EtapaRevertida(string statusAnterior, string statusNovo);
    void ErroRegistrado(string operacao, string tipoErro);
    void DuracaoEtapaRegistrada(string status, double duracaoMinutes);
    void OrdemCancelada(string statusAnterior);

    /// <summary>
    ///     Incrementa o contador de eventos de integração ignorados por já estarem no estado-destino
    ///     (idempotência cross-service). Disparado quando o consumer recebe a entrega tardia ou
    ///     duplicada de um evento e o agregado já avançou para além do estado-fonte esperado.
    /// </summary>
    /// <param name="evento">Nome do EntityName do integration event (ex.: "pagamento.confirmado.v1").</param>
    /// <param name="statusAtual">Status atual da OrdemDeServico no momento da skip.</param>
    void EventoIntegracaoIgnorado(string evento, string statusAtual);

    void DistribuicaoAtualizada(string statusNovo, string? statusAnterior);
}
