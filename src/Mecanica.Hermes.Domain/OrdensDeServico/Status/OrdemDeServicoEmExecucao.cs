using Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Status;

public class OrdemDeServicoEmExecucao : OrdemDeServicoStatusBase
{
    public override OrdemDeServicoStatus StatusAtual => OrdemDeServicoStatus.EmExecucao;
    protected internal override bool PermiteEditarProdutos => false;
    protected override OrdemDeServicoStatusBase ProximoStatus => new OrdemDeServicoManutencaoFinalizada();
    protected override OrdemDeServicoStatusBase StatusRevertido => new OrdemDeServicoAguardandoAprovacao();
    protected override OrdemDeServicoStatusBase StatusCancelado => new OrdemDeServicoCancelada();
}