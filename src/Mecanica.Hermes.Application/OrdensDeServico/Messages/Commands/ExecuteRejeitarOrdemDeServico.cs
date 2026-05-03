namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;

public sealed record ExecuteRejeitarOrdemDeServico(Guid OrdemDeServicoId) : IOrdemDeServicoExecuteCommand;
