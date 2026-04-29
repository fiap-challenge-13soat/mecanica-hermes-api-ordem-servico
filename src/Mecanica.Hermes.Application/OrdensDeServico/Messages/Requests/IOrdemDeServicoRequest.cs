namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

/// <summary>
/// Marca uma mensagem como uma requisição correlacionada por OrdemDeServicoId.
/// Cada request entra na SAGA <c>OrdemDeServicoStateMachine</c> e dispara, no máximo,
/// uma operação ativa por <see cref="OrdemDeServicoId"/>.
/// </summary>
public interface IOrdemDeServicoRequest
{
    Guid OrdemDeServicoId { get; }
}
