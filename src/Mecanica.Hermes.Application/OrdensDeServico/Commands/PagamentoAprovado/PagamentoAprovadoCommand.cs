using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Application.OrdensDeServico.Commands.PagamentoAprovado;

public sealed record PagamentoAprovadoCommand(Guid OrdemDeServicoId)
    : IAsyncCommand, IOrdemDeServicoCommand
{
    public object ToMessage()
    {
        return new PagamentoAprovadoRequest(OrdemDeServicoId);
    }
}
