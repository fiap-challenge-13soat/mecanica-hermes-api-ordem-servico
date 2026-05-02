using Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Status;

public class OrdemDeServicoManutencaoFinalizada : OrdemDeServicoStatusBase
{
    public override OrdemDeServicoStatus StatusAtual => OrdemDeServicoStatus.ManutencaoFinalizada;
    protected internal override bool PermiteEditarProdutos => false;
    protected override OrdemDeServicoStatusBase ProximoStatus => new OrdemDeServicoAguardandoPagamento();
    protected override OrdemDeServicoStatusBase StatusRevertido => new OrdemDeServicoEmExecucao();
    protected override OrdemDeServicoStatusBase StatusCancelado => new OrdemDeServicoCancelada();
}
