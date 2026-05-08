using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Application.OrdensDeServico.Commands.AddProduto;

public sealed record AddProdutoCommand(
    Guid OrdemDeServicoId,
    Guid ProdutoId,
    decimal ValorVenda,
    int Quantidade) : IAsyncCommand, IOrdemDeServicoCommand
{
    public object ToMessage()
    {
        return new AddProdutoRequest(OrdemDeServicoId, ProdutoId, ValorVenda, Quantidade);
    }
}
