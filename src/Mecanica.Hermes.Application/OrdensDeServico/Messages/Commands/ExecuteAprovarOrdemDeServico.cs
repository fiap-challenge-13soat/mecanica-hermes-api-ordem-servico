namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;

public sealed record ExecuteAprovarOrdemDeServico(Guid OrdemDeServicoId) : IOrdemDeServicoExecuteCommand;
