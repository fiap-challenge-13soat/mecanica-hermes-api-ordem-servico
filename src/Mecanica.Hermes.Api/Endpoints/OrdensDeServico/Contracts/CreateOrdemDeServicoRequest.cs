namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;

public sealed record CreateOrdemDeServicoRequest(
    Guid ClienteId,
    Guid VeiculoId,
    string? ProblemaRelatado,
    string? Observacoes);
