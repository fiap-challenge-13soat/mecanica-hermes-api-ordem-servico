using Mecanica.Hermes.Domain.Common.Results;

namespace Mecanica.Hermes.Domain.OrdensDeServico.ValueObjects;

public sealed class ValorVendaVo
{
    public ValorVendaVo()
    {
        // EF Construtor
    }

    private ValorVendaVo(decimal valor)
    {
        Valor = valor;
    }

    public decimal Valor { get; private set; }

    public static Result<ValorVendaVo> Criar(decimal valor)
    {
        if (valor < 0)
            return Result<ValorVendaVo>.BadRequest("Valor do produto não pode ser negativo.");

        var valorNormalizado = LimitarDuasCasasDecimais(valor);

        return Result<ValorVendaVo>.Ok(new ValorVendaVo(valorNormalizado));
    }

    private static decimal LimitarDuasCasasDecimais(decimal valor)
    {
        return Math.Floor(valor * 100) / 100;
    }
}
