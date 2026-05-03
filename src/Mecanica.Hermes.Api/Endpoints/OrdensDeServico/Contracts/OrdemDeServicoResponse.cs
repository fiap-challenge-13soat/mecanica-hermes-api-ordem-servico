using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;

public sealed record OrdemDeServicoResponse(
    Guid Id,
    Guid ClienteId,
    Guid VeiculoId,
    string? ProblemaRelatado,
    string? Observacoes,
    string StatusAtual,
    decimal ValorTotal,
    List<OrdemDeServicoProdutoResponse> Produtos,
    List<OrdemDeServicoServicoResponse> Servicos,
    List<OrdemDeServicoHistoricoStatusResponse> HistoricosStatus,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record OrdemDeServicoProdutoResponse(
    Guid ProdutoId,
    decimal Valor,
    int Quantidade);

public sealed record OrdemDeServicoServicoResponse(
    Guid ServicoId,
    decimal Valor);

public sealed record OrdemDeServicoHistoricoStatusResponse(
    OrdemDeServicoStatus StatusAtual,
    DateTime DataInicio,
    DateTime? DataFinalizacao);
