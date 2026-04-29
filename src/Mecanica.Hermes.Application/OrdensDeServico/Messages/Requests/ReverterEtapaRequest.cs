namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

public sealed record ReverterEtapaRequest(Guid OrdemDeServicoId) : IOrdemDeServicoRequest;
