namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;

public sealed record ExecuteCancelarOrdemDeServico(Guid OrdemDeServicoId) : IOrdemDeServicoExecuteCommand;
