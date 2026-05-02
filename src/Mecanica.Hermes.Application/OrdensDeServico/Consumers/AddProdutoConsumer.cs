using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.Common.Persistence;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.Common.Results;
using Mecanica.Hermes.Domain.OrdensDeServico;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers;

public sealed class AddProdutoConsumer(IOrdemDeServicoRepository repository, IUnitOfWork uow)
    : OrdemDeServicoMutateConsumerBase<ExecuteAddProduto>(repository, uow)
{
    protected override Result Execute(OrdemDeServico ordem, ExecuteAddProduto msg)
    {
        return ordem.AdicionarProduto(msg.ProdutoId, msg.ValorVenda, msg.Quantidade);
    }
}
