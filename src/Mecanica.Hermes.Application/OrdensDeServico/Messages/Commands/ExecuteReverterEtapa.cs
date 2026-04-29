namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;

public sealed record ExecuteReverterEtapa(Guid OrdemDeServicoId) : IOrdemDeServicoExecuteCommand;
