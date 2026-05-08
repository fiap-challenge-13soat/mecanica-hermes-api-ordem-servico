using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;
using Mecanica.Hermes.Shared.Application.Messaging;

namespace Mecanica.Hermes.Application.OrdensDeServico.Commands.ReverterEtapa;

public sealed record ReverterEtapaCommand(Guid OrdemDeServicoId)
    : IAsyncCommand, IOrdemDeServicoCommand
{
    public object ToMessage()
    {
        return new ReverterEtapaRequest(OrdemDeServicoId);
    }
}
