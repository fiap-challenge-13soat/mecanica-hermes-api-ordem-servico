using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;
using Mecanica.Hermes.Application.OrdensDeServico.Dtos;
using Mecanica.Hermes.Shared.Core.Pagination;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Mappings;

internal static class OrdemDeServicoMappingExtensions
{
    internal static OrdemDeServicoResponse ToResponse(this OrdemDeServicoDto dto)
    {
        return new OrdemDeServicoResponse(
            dto.Id,
            dto.ClienteId,
            dto.VeiculoId,
            dto.ProblemaRelatado,
            dto.Observacoes,
            dto.StatusAtual,
            dto.ValorTotal,
            dto.Produtos.Select(p => p.ToResponse()).ToList(),
            dto.Servicos.Select(s => s.ToResponse()).ToList(),
            dto.HistoricosStatus.Select(h => h.ToResponse()).ToList(),
            dto.CreatedAt,
            dto.UpdatedAt);
    }

    internal static OrdemDeServicoProdutoResponse ToResponse(this OrdemDeServicoProdutoDto dto)
    {
        return new OrdemDeServicoProdutoResponse(dto.ProdutoId, dto.Valor, dto.Quantidade);
    }

    internal static OrdemDeServicoServicoResponse ToResponse(this OrdemDeServicoServicoDto dto)
    {
        return new OrdemDeServicoServicoResponse(dto.ServicoId, dto.Valor);
    }

    internal static OrdemDeServicoHistoricoStatusResponse ToResponse(this OrdemDeServicoHistoricoStatusDto dto)
    {
        return new OrdemDeServicoHistoricoStatusResponse(dto.StatusAtual, dto.DataInicio, dto.DataFinalizacao);
    }

    internal static PagedResult<OrdemDeServicoResponse> ToResponse(this PagedResult<OrdemDeServicoDto> pagedDto)
    {
        return PagedResult<OrdemDeServicoResponse>.Create(
            pagedDto.Items.Select(dto => dto.ToResponse()),
            pagedDto.Page,
            pagedDto.PageSize,
            pagedDto.TotalCount);
    }
}
