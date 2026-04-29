namespace Mecanica.Hermes.Application.OrdensDeServico.Dtos;

public sealed record OrdemDeServicoDto(
    Guid Id,
    Guid ClienteId,
    Guid VeiculoId,
    string? ProblemaRelatado,
    string? Observacoes,
    string StatusAtual,
    decimal ValorTotal,
    List<OrdemDeServicoProdutoDto> Produtos,
    List<OrdemDeServicoServicoDto> Servicos,
    IList<OrdemDeServicoHistoricoStatusDto> HistoricosStatus,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
