namespace Mecanica.Hermes.Application.OrdensDeServico.Ports;

public interface IOrdemDeServicoMetrics
{
    void OrdemCriada();
    void EtapaAvancada(string statusAnterior, string statusNovo);
    void EtapaRevertida(string statusAnterior, string statusNovo);
    void ErroRegistrado(string operacao, string tipoErro);
    void DuracaoEtapaRegistrada(string status, double duracaoMinutes);
    void OrdemCancelada(string statusAnterior);
}
