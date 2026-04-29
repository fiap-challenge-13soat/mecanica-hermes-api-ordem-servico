namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;

public sealed record ExecutePagamentoAprovado(Guid OrdemDeServicoId) : IOrdemDeServicoExecuteCommand;
