using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Domain.Common.Results;
using Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Status;

public class OrdemDeServicoRecebida : OrdemDeServicoStatusBase
{
    public override OrdemDeServicoStatus StatusAtual => OrdemDeServicoStatus.Recebida;
    protected internal override bool PermiteEditarProdutos => true;
    protected override OrdemDeServicoStatusBase ProximoStatus => new OrdemDeServicoEmDiagnostico();
    protected override OrdemDeServicoStatusBase StatusRevertido => this;
    protected override OrdemDeServicoStatusBase StatusCancelado => new OrdemDeServicoCancelada();

    public override Result<OrdemDeServicoStatusBase> ReverterEtapa(Action<IDomainEvent> addDomainEvent)
    {
        return Result<OrdemDeServicoStatusBase>.BadRequest("Ordem de serviço recebida, não pode reverter para a etapa anterior.");
    }
}