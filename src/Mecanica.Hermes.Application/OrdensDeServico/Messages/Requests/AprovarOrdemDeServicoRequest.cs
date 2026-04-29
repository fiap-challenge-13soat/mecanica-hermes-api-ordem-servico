namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

public sealed record AprovarOrdemDeServicoRequest(Guid OrdemDeServicoId) : IOrdemDeServicoRequest;
