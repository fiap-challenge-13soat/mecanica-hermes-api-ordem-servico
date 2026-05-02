using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;

public interface IOrdemDeServicoStatus
{
    OrdemDeServicoStatus StatusAtual { get; }
    OrdemDeServicoStatus StatusDestino { get; }
    DateTime DataInicio { get; }
    DateTime? DataFinalizacao { get; }
}
