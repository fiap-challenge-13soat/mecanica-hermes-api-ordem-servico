using Mecanica.Hermes.Domain.OrdensDeServico.ValueObjects;

namespace Mecanica.Hermes.Infrastructure.Persistence.Configurations.Helpers;

internal static class ValorVendaVoHelper
{
    public static ValorVendaVo RecriarValor(decimal valor)
    {
        var resultado = ValorVendaVo.Criar(valor);
        if (resultado.IsSuccess && resultado.Data is not null)
            return resultado.Data;

        throw new InvalidOperationException("Não foi possível reconstruir o valor de venda armazenado.");
    }
}