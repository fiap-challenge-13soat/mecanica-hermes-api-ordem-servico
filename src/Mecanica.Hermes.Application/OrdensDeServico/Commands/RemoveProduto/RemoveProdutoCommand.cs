using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;
using Mecanica.Hermes.Shared.Application.Messaging;

namespace Mecanica.Hermes.Application.OrdensDeServico.Commands.RemoveProduto;

public sealed record RemoveProdutoCommand(
    Guid OrdemDeServicoId,
    Guid ProdutoId) : IAsyncCommand, IOrdemDeServicoCommand
{
    public object ToMessage()
    {
        return new RemoveProdutoRequest(OrdemDeServicoId, ProdutoId);
    }
}
