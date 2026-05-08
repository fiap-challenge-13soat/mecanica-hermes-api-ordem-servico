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

public sealed class PagamentoAprovadoConsumer(
    IOrdemDeServicoRepository repository,
    IUnitOfWork uow,
    IOrdemDeServicoMetrics metrics)
    : OrdemDeServicoMutateConsumerBase<ExecutePagamentoAprovado>(repository, uow, metrics)
{
    protected override string OperacaoMetricKey => OrdemDeServicoMetricKeys.Operations.PagamentoAprovado;

    protected override Result ValidatePreconditions(OrdemDeServico ordem, ExecutePagamentoAprovado msg)
    {
        return ordem.StatusAtual.StatusAtual == OrdemDeServicoStatus.AguardandoPagamento
            ? Result.Ok()
            : Result.BadRequest("Ordem de serviço precisa estar em aguardando pagamento para confirmar pagamento.");
    }

    protected override Result Execute(OrdemDeServico ordem, ExecutePagamentoAprovado msg)
    {
        return ordem.AvancarEtapa();
    }
}
