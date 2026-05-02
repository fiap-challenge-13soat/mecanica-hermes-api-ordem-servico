using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Domain.Common.Results;
using Mecanica.Hermes.Domain.OrdensDeServico.ValueObjects;

namespace Mecanica.Hermes.Domain.OrdensDeServico;

public class OrdemDeServicoServico : BaseDomain
{
    internal OrdemDeServicoServico(Guid servicoId, ValorVendaVo valorVenda)
    {
        ServicoId = servicoId;
        ValorVenda = valorVenda;
    }

    public Guid ServicoId { get; private set; }
    public ValorVendaVo ValorVenda { get; private set; }

    internal static Result<OrdemDeServicoServico> Criar(Guid servicoId, decimal valorVenda)
    {
        var errors = new List<string>();

        var valorResult = ValorVendaVo.Criar(valorVenda);
        if (valorResult.IsFailure)
            errors.AddRange(valorResult.Errors);

        if (errors.Count > 0)
            return Result<OrdemDeServicoServico>.BadRequest(errors);

        var servico = new OrdemDeServicoServico(servicoId, valorResult.Data!);

        return Result<OrdemDeServicoServico>.Ok(servico);
    }

    internal static OrdemDeServicoServico Restaurar(
        Guid id,
        Guid servicoId,
        ValorVendaVo valor,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        var item = new OrdemDeServicoServico(servicoId, valor);
        item.RestaurarBase(id, createdAt, updatedAt);
        return item;
    }
}
