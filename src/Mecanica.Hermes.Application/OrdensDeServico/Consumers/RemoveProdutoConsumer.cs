using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Shared.Core.Persistence;
using Mecanica.Hermes.Shared.Core.Results;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers;

public sealed class RemoveProdutoConsumer(IOrdemDeServicoRepository repository, IUnitOfWork uow)
    : OrdemDeServicoMutateConsumerBase<ExecuteRemoveProduto>(repository, uow)
{
    protected override Result Execute(OrdemDeServico ordem, ExecuteRemoveProduto msg)
    {
        return ordem.RemoverProduto(msg.ProdutoId);
    }
}
