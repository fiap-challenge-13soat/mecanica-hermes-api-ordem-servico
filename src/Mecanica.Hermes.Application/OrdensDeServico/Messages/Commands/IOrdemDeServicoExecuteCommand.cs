namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;

/// <summary>
///     Marca um comando despachado pela SAGA para um consumer especializado.
///     O <see cref="OrdemDeServicoId" /> é o mesmo CorrelationId da SAGA, o que permite
///     que o consumer publique <c>OrdemDeServicoOperationCompleted/Failed</c> de volta para a SAGA.
/// </summary>
public interface IOrdemDeServicoExecuteCommand
{
    Guid OrdemDeServicoId { get; }
}
