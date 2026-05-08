using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Application.OrdensDeServico.Commands.RejeitarOrdemDeServico;

public sealed record RejeitarOrdemDeServicoCommand(Guid OrdemDeServicoId)
    : IAsyncCommand, IOrdemDeServicoCommand
{
    public object ToMessage()
    {
        return new RejeitarOrdemDeServicoRequest(OrdemDeServicoId);
    }
}
