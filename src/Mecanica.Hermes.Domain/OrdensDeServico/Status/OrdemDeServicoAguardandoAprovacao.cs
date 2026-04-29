using Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Status;

public class OrdemDeServicoAguardandoAprovacao : OrdemDeServicoStatusBase
{
    public override OrdemDeServicoStatus StatusAtual => OrdemDeServicoStatus.AguardandoAprovacao;
    protected internal override bool PermiteEditarProdutos => false;
    protected override OrdemDeServicoStatusBase ProximoStatus => new OrdemDeServicoEmExecucao();
    protected override OrdemDeServicoStatusBase StatusRevertido => new OrdemDeServicoEmDiagnostico();
    protected override OrdemDeServicoStatusBase StatusCancelado => new OrdemDeServicoRejeitada();
}