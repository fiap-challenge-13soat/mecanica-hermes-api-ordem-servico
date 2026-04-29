using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.Common.Persistence;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.Common.Results;
using Mecanica.Hermes.Domain.OrdensDeServico;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers;

public sealed class RemoveServicoConsumer(IOrdemDeServicoRepository repository, IUnitOfWork uow)
    : OrdemDeServicoMutateConsumerBase<ExecuteRemoveServico>(repository, uow)
{
    protected override Result Execute(OrdemDeServico ordem, ExecuteRemoveServico msg) =>
        ordem.RemoverServico(msg.ServicoId);
}
