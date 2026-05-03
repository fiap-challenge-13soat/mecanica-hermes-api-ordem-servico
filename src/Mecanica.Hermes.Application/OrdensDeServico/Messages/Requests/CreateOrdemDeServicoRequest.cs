namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

public sealed record CreateOrdemDeServicoRequest(
    Guid OrdemDeServicoId,
    Guid ClienteId,
    Guid VeiculoId,
    string? ProblemaRelatado,
    string? Observacoes) : IOrdemDeServicoRequest;
