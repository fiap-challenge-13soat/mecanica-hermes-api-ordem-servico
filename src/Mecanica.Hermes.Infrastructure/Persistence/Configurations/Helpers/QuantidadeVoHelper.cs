using Mecanica.Hermes.Domain.OrdensDeServico.ValueObjects;

namespace Mecanica.Hermes.Infrastructure.Persistence.Configurations.Helpers;

internal static class QuantidadeVoHelper
{
    public static QuantidadeVo RecriarQuantidade(int valor)
    {
        var resultado = QuantidadeVo.Criar(valor);
        if (resultado.IsSuccess && resultado.Data is not null)
            return resultado.Data;

        throw new InvalidOperationException("Não foi possível reconstruir a quantidade do produto armazenada.");
    }
}
