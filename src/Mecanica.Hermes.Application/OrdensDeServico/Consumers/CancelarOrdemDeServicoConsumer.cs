using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Shared.Core.Persistence;
using Mecanica.Hermes.Shared.Core.Results;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers;

public sealed class CancelarOrdemDeServicoConsumer(IOrdemDeServicoRepository repository, IUnitOfWork uow)
    : OrdemDeServicoMutateConsumerBase<ExecuteCancelarOrdemDeServico>(repository, uow)
{
    protected override Result Execute(OrdemDeServico ordem, ExecuteCancelarOrdemDeServico msg)
    {
        return ordem.Cancelar();
    }
}
