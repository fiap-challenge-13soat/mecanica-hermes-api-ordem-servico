namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

public sealed record AvancarEtapaRequest(Guid OrdemDeServicoId) : IOrdemDeServicoRequest;
