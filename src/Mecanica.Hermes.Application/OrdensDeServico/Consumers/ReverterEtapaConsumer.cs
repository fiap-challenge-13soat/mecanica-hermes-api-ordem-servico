using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Shared.Core.Persistence;
using Mecanica.Hermes.Application.OrdensDeServico.Constants;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Shared.Core.Results;
using Mecanica.Hermes.Domain.OrdensDeServico;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers;

public sealed class ReverterEtapaConsumer(
    IOrdemDeServicoRepository repository,
    IUnitOfWork uow,
    IOrdemDeServicoMetrics metrics)
    : OrdemDeServicoMutateConsumerBase<ExecuteReverterEtapa>(repository, uow, metrics)
{
    protected override string OperacaoMetricKey => OrdemDeServicoMetricKeys.Operations.ReverterEtapa;

    protected override Result Execute(OrdemDeServico ordem, ExecuteReverterEtapa msg)
    {
        return ordem.ReverterEtapa();
    }
}
