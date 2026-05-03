namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

public sealed record PagamentoAprovadoRequest(Guid OrdemDeServicoId) : IOrdemDeServicoRequest;
