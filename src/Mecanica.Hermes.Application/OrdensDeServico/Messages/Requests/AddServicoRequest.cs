namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

public sealed record AddServicoRequest(
    Guid OrdemDeServicoId,
    Guid ServicoId,
    decimal ValorVenda) : IOrdemDeServicoRequest;
