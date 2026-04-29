namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;

public sealed record ExecuteCreateOrdemDeServico(
    Guid OrdemDeServicoId,
    Guid ClienteId,
    Guid VeiculoId,
    string? ProblemaRelatado,
    string? Observacoes) : IOrdemDeServicoExecuteCommand;
