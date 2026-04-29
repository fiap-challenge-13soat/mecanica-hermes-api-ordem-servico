using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Application.OrdensDeServico.Commands.CreateOrdemDeServico;

/// <summary>
/// O <see cref="OrdemDeServicoId"/> é gerado pelo cliente (camada de API) antes de despachar
/// o comando, permitindo que a resposta 202 Accepted carregue o Id mesmo antes da
/// persistência assíncrona ocorrer.
/// </summary>
public sealed record CreateOrdemDeServicoCommand(
    Guid OrdemDeServicoId,
    Guid ClienteId,
    Guid VeiculoId,
    string? ProblemaRelatado,
    string? Observacoes) : IAsyncCommand, IOrdemDeServicoCommand
{
    public object ToMessage() => new CreateOrdemDeServicoRequest(
        OrdemDeServicoId,
        ClienteId,
        VeiculoId,
        ProblemaRelatado,
        Observacoes);
}
