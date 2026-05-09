using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Constants;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Shared.Core.Persistence;
using Mecanica.Hermes.Shared.Core.Results;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers;

/// <summary>
///     Consumer que cria uma nova <see cref="OrdemDeServico" /> via <c>AddAsync</c>.
///     O pipeline (validação → persistência → publicação) é fornecido pela
///     <see cref="OrdemDeServicoCreateConsumerBase{TCommand}" />.
/// </summary>
public sealed class CreateOrdemDeServicoConsumer
    : OrdemDeServicoCreateConsumerBase<ExecuteCreateOrdemDeServico>
{
    public CreateOrdemDeServicoConsumer(
        IOrdemDeServicoRepository repository,
        IUnitOfWork uow,
        IOrdemDeServicoMetrics metrics)
        : base(repository, uow, metrics)
    {
    }

    protected override string OperacaoMetricKey => OrdemDeServicoMetricKeys.Operations.Criar;

    protected override Result<OrdemDeServico> CreateAggregate(ExecuteCreateOrdemDeServico msg)
    {
        return OrdemDeServico.Criar(
            msg.OrdemDeServicoId,
            msg.ClienteId,
            msg.VeiculoId,
            msg.ProblemaRelatado,
            msg.Observacoes);
    }
}
