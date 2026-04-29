using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Application.OrdensDeServico.Commands.AddServico;

public sealed record AddServicoCommand(
    Guid OrdemDeServicoId,
    Guid ServicoId,
    decimal ValorVenda) : IAsyncCommand, IOrdemDeServicoCommand
{
    public object ToMessage() => new AddServicoRequest(OrdemDeServicoId, ServicoId, ValorVenda);
}
