using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Constants;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Shared.Core.Persistence;
using Mecanica.Hermes.Shared.Core.Results;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers;

public sealed class AprovarOrdemDeServicoConsumer(
    IOrdemDeServicoRepository repository,
    IUnitOfWork uow,
    IOrdemDeServicoMetrics metrics)
    : OrdemDeServicoMutateConsumerBase<ExecuteAprovarOrdemDeServico>(repository, uow, metrics)
{
    protected override string OperacaoMetricKey => OrdemDeServicoMetricKeys.Operations.Aprovar;

    protected override Result ValidatePreconditions(OrdemDeServico ordem, ExecuteAprovarOrdemDeServico msg)
    {
        return ordem.StatusAtual.StatusAtual == OrdemDeServicoStatus.AguardandoAprovacao
            ? Result.Ok()
            : Result.BadRequest("Ordem de serviço precisa estar em aguardando aprovação para ser aprovada.");
    }

    protected override Result Execute(OrdemDeServico ordem, ExecuteAprovarOrdemDeServico msg)
    {
        return ordem.AvancarEtapa();
    }
}
