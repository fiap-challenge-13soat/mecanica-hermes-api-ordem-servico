using Mecanica.Hermes.Shared.Core.Domain;
using Mecanica.Hermes.Shared.Core.Results;
using Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Status;

public class OrdemDeServicoRejeitada : OrdemDeServicoStatusBase
{
    public override OrdemDeServicoStatus StatusAtual => OrdemDeServicoStatus.Rejeitada;
    protected internal override bool PermiteEditarProdutos => false;
    protected override OrdemDeServicoStatusBase ProximoStatus => this;
    protected override OrdemDeServicoStatusBase StatusRevertido => new OrdemDeServicoAguardandoAprovacao();
    protected override OrdemDeServicoStatusBase StatusCancelado => new OrdemDeServicoCancelada();

    public override Result<OrdemDeServicoStatusBase> AvancarEtapa(Action<IDomainEvent> addDomainEvent)
    {
        return Result<OrdemDeServicoStatusBase>.BadRequest(
            "Ordem de serviço rejeitada, não pode avançar para a próxima etapa.");
    }
}
