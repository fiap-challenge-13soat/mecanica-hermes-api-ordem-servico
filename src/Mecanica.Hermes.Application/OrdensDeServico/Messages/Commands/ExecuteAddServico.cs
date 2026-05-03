namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;

public sealed record ExecuteAddServico(
    Guid OrdemDeServicoId,
    Guid ServicoId,
    decimal ValorVenda) : IOrdemDeServicoExecuteCommand;
