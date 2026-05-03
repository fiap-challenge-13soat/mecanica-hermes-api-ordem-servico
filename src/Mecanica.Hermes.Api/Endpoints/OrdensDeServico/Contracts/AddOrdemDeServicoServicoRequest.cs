namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;

public sealed record AddOrdemDeServicoServicoRequest(Guid ServicoId, decimal Valor);
