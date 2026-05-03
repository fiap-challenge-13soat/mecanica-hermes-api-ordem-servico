namespace Mecanica.Hermes.Domain.OrdensDeServico.Enums;

public enum OrdemDeServicoStatus
{
    Recebida,
    EmDiagnostico,
    AguardandoAprovacao,
    Rejeitada,
    EmExecucao,
    ManutencaoFinalizada,
    AguardandoPagamento,
    PagamentoConfirmado,
    Entregue,
    Cancelada
}
