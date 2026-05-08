using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Shared.Core.Persistence;
using Mecanica.Hermes.Application.OrdensDeServico.Constants;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Shared.Core.Results;
using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers;

public sealed class RejeitarOrdemDeServicoConsumer(
    IOrdemDeServicoRepository repository,
    IUnitOfWork uow,
    IOrdemDeServicoMetrics metrics)
    : OrdemDeServicoMutateConsumerBase<ExecuteRejeitarOrdemDeServico>(repository, uow, metrics)
{
    protected override string OperacaoMetricKey => OrdemDeServicoMetricKeys.Operations.Rejeitar;

    protected override Result ValidatePreconditions(OrdemDeServico ordem, ExecuteRejeitarOrdemDeServico msg)
    {
        return ordem.StatusAtual.StatusAtual == OrdemDeServicoStatus.AguardandoAprovacao
            ? Result.Ok()
            : Result.BadRequest("Ordem de serviço precisa estar em aguardando aprovação para ser rejeitada.");
    }

    protected override Result Execute(OrdemDeServico ordem, ExecuteRejeitarOrdemDeServico msg)
    {
        return ordem.Cancelar();
    }
}
