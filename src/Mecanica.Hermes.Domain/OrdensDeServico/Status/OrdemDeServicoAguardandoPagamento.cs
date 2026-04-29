using Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Status;

public class OrdemDeServicoAguardandoPagamento: OrdemDeServicoStatusBase
{
    public override OrdemDeServicoStatus StatusAtual  => OrdemDeServicoStatus.AguardandoPagamento;
    protected internal override bool PermiteEditarProdutos  => false;
    protected override OrdemDeServicoStatusBase ProximoStatus => new OrdemDeServicoPagamentoConfirmado();
    protected override OrdemDeServicoStatusBase StatusRevertido => new OrdemDeServicoManutencaoFinalizada();
    protected override OrdemDeServicoStatusBase StatusCancelado => new OrdemDeServicoCancelada();
}