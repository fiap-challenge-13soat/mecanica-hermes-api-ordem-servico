namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Events;

/// <summary>
/// Publicado por um consumer quando a operação falha (regra de negócio violada,
/// ordem inexistente, etc). A SAGA volta para <c>Initial</c>, registrando a falha.
/// </summary>
public sealed record OrdemDeServicoOperationFailed(
    Guid OrdemDeServicoId,
    string Operacao,
    string Motivo);
