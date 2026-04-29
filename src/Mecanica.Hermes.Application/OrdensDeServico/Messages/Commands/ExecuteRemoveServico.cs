namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;

public sealed record ExecuteRemoveServico(
    Guid OrdemDeServicoId,
    Guid ServicoId) : IOrdemDeServicoExecuteCommand;
