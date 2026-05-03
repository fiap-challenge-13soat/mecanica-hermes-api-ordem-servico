namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;

public sealed record ExecuteAvancarEtapa(Guid OrdemDeServicoId) : IOrdemDeServicoExecuteCommand;
