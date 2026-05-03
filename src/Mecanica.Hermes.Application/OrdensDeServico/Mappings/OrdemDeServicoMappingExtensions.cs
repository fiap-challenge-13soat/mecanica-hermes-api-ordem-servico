using Mecanica.Hermes.Application.OrdensDeServico.Dtos;
using Mecanica.Hermes.Domain.OrdensDeServico;

namespace Mecanica.Hermes.Application.OrdensDeServico.Mappings;

internal static class OrdemDeServicoMappingExtensions
{
    internal static OrdemDeServicoDto ToDto(this OrdemDeServico ordemDeServico)
    {
        return new OrdemDeServicoDto(
            ordemDeServico.Id,
            ordemDeServico.ClienteId,
            ordemDeServico.VeiculoId,
            ordemDeServico.ProblemaRelatado,
            ordemDeServico.Observacoes,
            ordemDeServico.StatusAtual.StatusAtual.ToString(),
            ordemDeServico.ValorTotal,
            ordemDeServico.Produtos.Select(p => p.ToDto()).ToList(),
            ordemDeServico.Servicos.Select(s => s.ToDto()).ToList(),
            ordemDeServico.HistoricosStatus.Select(h => h.ToDto()).ToList(),
            ordemDeServico.CreatedAt,
            ordemDeServico.UpdatedAt);
    }

    internal static OrdemDeServicoProdutoDto ToDto(this OrdemDeServicoProduto produto)
    {
        return new OrdemDeServicoProdutoDto(
            produto.Id,
            produto.ProdutoId,
            produto.ValorVenda.Valor,
            produto.Quantidade.Valor);
    }

    internal static OrdemDeServicoServicoDto ToDto(this OrdemDeServicoServico servico)
    {
        return new OrdemDeServicoServicoDto(
            servico.Id,
            servico.ServicoId,
            servico.ValorVenda.Valor);
    }

    internal static OrdemDeServicoHistoricoStatusDto ToDto(this OrdemDeServicoHistoricoStatus historico)
    {
        return new OrdemDeServicoHistoricoStatusDto(
            historico.StatusAnterior,
            historico.StatusAtual,
            historico.StatusDestino,
            historico.DataInicio,
            historico.DataFinalizacao,
            historico.CreatedAt,
            historico.UpdatedAt);
    }
}
