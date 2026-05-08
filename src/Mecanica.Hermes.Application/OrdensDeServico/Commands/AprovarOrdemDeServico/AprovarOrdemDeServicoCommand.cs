using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Application.OrdensDeServico.Commands.AprovarOrdemDeServico;

public sealed record AprovarOrdemDeServicoCommand(Guid OrdemDeServicoId)
    : IAsyncCommand, IOrdemDeServicoCommand
{
    public object ToMessage()
    {
        return new AprovarOrdemDeServicoRequest(OrdemDeServicoId);
    }
}
