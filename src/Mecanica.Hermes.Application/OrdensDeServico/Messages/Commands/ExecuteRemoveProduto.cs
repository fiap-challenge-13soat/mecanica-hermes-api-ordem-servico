namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;

public sealed record ExecuteRemoveProduto(
    Guid OrdemDeServicoId,
    Guid ProdutoId) : IOrdemDeServicoExecuteCommand;
