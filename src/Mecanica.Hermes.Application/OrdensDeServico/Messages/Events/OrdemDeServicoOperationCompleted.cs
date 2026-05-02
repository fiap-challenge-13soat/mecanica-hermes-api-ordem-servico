namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Events;

/// <summary>
///     Publicado por um consumer após processar com sucesso uma operação solicitada pela SAGA.
///     Faz a SAGA voltar do estado <c>Processing</c> para <c>Initial</c>, permitindo
///     novas requisições para a mesma <see cref="OrdemDeServicoId" />.
/// </summary>
public sealed record OrdemDeServicoOperationCompleted(
    Guid OrdemDeServicoId,
    string Operacao);
