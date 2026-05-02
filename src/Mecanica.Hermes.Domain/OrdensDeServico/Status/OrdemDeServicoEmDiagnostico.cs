using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Domain.Common.Results;
using Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Status;

public class OrdemDeServicoEmDiagnostico : OrdemDeServicoStatusBase
{
    public override OrdemDeServicoStatus StatusAtual => OrdemDeServicoStatus.EmDiagnostico;
    protected internal override bool PermiteEditarProdutos => true;
    protected override OrdemDeServicoStatusBase ProximoStatus => new OrdemDeServicoAguardandoAprovacao();
    protected override OrdemDeServicoStatusBase StatusRevertido => new OrdemDeServicoRecebida();
    protected override OrdemDeServicoStatusBase StatusCancelado => new OrdemDeServicoCancelada();

    public override Result<OrdemDeServicoStatusBase> AvancarEtapa(Action<IDomainEvent> addDomainEvent)
    {
        return base.AvancarEtapa(addDomainEvent);
    }
}
