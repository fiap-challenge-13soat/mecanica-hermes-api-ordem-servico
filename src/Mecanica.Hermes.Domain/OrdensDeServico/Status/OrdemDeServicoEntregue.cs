using Mecanica.Hermes.Shared.Core.Domain;
using Mecanica.Hermes.Shared.Core.Results;
using Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Status;

public class OrdemDeServicoEntregue : OrdemDeServicoStatusBase
{
    public override OrdemDeServicoStatus StatusAtual => OrdemDeServicoStatus.Entregue;
    protected internal override bool PermiteEditarProdutos => false;
    protected override OrdemDeServicoStatusBase ProximoStatus => this;
    protected override OrdemDeServicoStatusBase StatusRevertido => new OrdemDeServicoPagamentoConfirmado();
    protected override OrdemDeServicoStatusBase StatusCancelado => new OrdemDeServicoCancelada();

    public override Result<OrdemDeServicoStatusBase> AvancarEtapa(Action<IDomainEvent> addDomainEvent)
    {
        return Result<OrdemDeServicoStatusBase>.BadRequest(
            "Ordem de serviço entregue, não pode avançar para a próxima etapa.");
    }
}
