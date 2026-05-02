using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Domain.OrdensDeServico;

public class OrdemDeServicoHistoricoStatus : BaseDomain, IOrdemDeServicoStatus
{
    private OrdemDeServicoHistoricoStatus()
    {
        // EF Constructor
    }

    /// <summary>
    ///     Cria um item de histórico arquivando o status sob transição.
    /// </summary>
    /// <param name="status">Status que está sendo arquivado (período encerrando).</param>
    /// <param name="statusAnterior">
    ///     Status que precedeu este na linha do tempo da OS. Para o primeiro item do histórico,
    ///     passe o próprio <c>status.StatusAtual</c> indicando a ausência de predecessor.
    /// </param>
    internal OrdemDeServicoHistoricoStatus(IOrdemDeServicoStatus status, OrdemDeServicoStatus statusAnterior)
    {
        StatusAnterior = statusAnterior;
        StatusAtual = status.StatusAtual;
        StatusDestino = status.StatusDestino;
        DataInicio = status.DataInicio;
        DataFinalizacao = status.DataFinalizacao;
    }

    public OrdemDeServicoStatus StatusAnterior { get; internal set; }
    public OrdemDeServicoStatus StatusAtual { get; internal set; }
    public OrdemDeServicoStatus StatusDestino { get; internal set; }
    public DateTime DataInicio { get; internal set; }
    public DateTime? DataFinalizacao { get; internal set; }

    internal static OrdemDeServicoHistoricoStatus Restaurar(
        Guid id,
        OrdemDeServicoStatus statusAnterior,
        OrdemDeServicoStatus statusAtual,
        OrdemDeServicoStatus statusDestino,
        DateTime dataInicio,
        DateTime? dataFinalizacao,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        var item = new OrdemDeServicoHistoricoStatus
        {
            StatusAnterior = statusAnterior,
            StatusAtual = statusAtual,
            StatusDestino = statusDestino,
            DataInicio = dataInicio,
            DataFinalizacao = dataFinalizacao
        };
        item.RestaurarBase(id, createdAt, updatedAt);
        return item;
    }
}
