using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Application.OrdensDeServico.Commands.CancelarOrdemDeServico;

public sealed record CancelarOrdemDeServicoCommand(Guid OrdemDeServicoId)
    : IAsyncCommand, IOrdemDeServicoCommand
{
    public object ToMessage() => new CancelarOrdemDeServicoRequest(OrdemDeServicoId);
}
