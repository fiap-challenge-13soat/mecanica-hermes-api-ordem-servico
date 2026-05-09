using Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Shared.Core.Domain;
using Mecanica.Hermes.Shared.Core.Results;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Status;

public class OrdemDeServicoCancelada : OrdemDeServicoStatusBase
{
    public override OrdemDeServicoStatus StatusAtual => OrdemDeServicoStatus.Cancelada;
    protected internal override bool PermiteEditarProdutos => false;
    protected override OrdemDeServicoStatusBase ProximoStatus => this;
    protected override OrdemDeServicoStatusBase StatusRevertido => this;
    protected override OrdemDeServicoStatusBase StatusCancelado => this;

    public override Result<OrdemDeServicoStatusBase> AvancarEtapa(Action<IDomainEvent> addDomainEvent)
    {
        return Result<OrdemDeServicoStatusBase>.BadRequest(
            "Ordem de serviço cancelada, não pode avançar para a próxima etapa.");
    }

    public override Result<OrdemDeServicoStatusBase> ReverterEtapa(Action<IDomainEvent> addDomainEvent)
    {
        return Result<OrdemDeServicoStatusBase>.BadRequest(
            "Ordem de serviço cancelada, não pode reverter para a etapa anterior.");
    }

    public override Result<OrdemDeServicoStatusBase> Cancelar(Action<IDomainEvent> addDomainEvent)
    {
        return Result<OrdemDeServicoStatusBase>.BadRequest(
            "Ordem de serviço já cancelada, não pode ser cancelada novamente.");
    }
}
