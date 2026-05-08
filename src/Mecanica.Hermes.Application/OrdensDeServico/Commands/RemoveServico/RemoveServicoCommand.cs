using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;
using Mecanica.Hermes.Shared.Application.Messaging;

namespace Mecanica.Hermes.Application.OrdensDeServico.Commands.RemoveServico;

public sealed record RemoveServicoCommand(
    Guid OrdemDeServicoId,
    Guid ServicoId) : IAsyncCommand, IOrdemDeServicoCommand
{
    public object ToMessage()
    {
        return new RemoveServicoRequest(OrdemDeServicoId, ServicoId);
    }
}
