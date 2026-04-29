namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;

/// <summary>
/// Resposta retornada para a criação assíncrona de uma ordem de serviço.
/// O <see cref="Id"/> é gerado pela API antes de a mensagem ser enfileirada,
/// permitindo que o cliente consulte o agregado em <c>GET /ordens-de-servico/{id}</c>
/// assim que o consumer terminar a persistência.
/// </summary>
public sealed record CreateOrdemDeServicoAcceptedResponse(Guid Id);
