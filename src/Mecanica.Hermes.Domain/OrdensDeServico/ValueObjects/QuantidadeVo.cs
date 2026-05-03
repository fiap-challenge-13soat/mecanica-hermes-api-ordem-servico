using System.Globalization;
using Mecanica.Hermes.Domain.Common.Results;

namespace Mecanica.Hermes.Domain.OrdensDeServico.ValueObjects;

public sealed class QuantidadeVo
{
    public QuantidadeVo()
    {
        // EF Construtor
    }

    public QuantidadeVo(int valor)
    {
        Valor = valor;
    }

    public int Valor { get; }

    public override string ToString()
    {
        return Valor.ToString(CultureInfo.InvariantCulture);
    }

    public static Result<QuantidadeVo> Criar(int valor)
    {
        if (valor <= 0)
            return Result<QuantidadeVo>.BadRequest("A quantidade do produto deve ser positiva.");

        return Result<QuantidadeVo>.Ok(new QuantidadeVo(valor));
    }
}
