using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Shared.Core.Domain;
using Mecanica.Hermes.Shared.Core.Results;

namespace Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;

/// <summary>
///     Implementação do State Pattern para o ciclo de vida da <c>OrdemDeServico</c>.
///     A mutação das propriedades de estado (<see cref="StatusDestino" />, <see cref="DataInicio" />,
///     <see cref="DataFinalizacao" />) ocorre exclusivamente:
///     - via <see cref="AvancarEtapa" />, <see cref="ReverterEtapa" /> e <see cref="Cancelar" />;
///     - via <see cref="Restaurar" />, de uso interno do mapper de persistência.
/// </summary>
public abstract class OrdemDeServicoStatusBase : BaseDomain, IOrdemDeServicoStatus
{
    protected internal abstract bool PermiteEditarProdutos { get; }
    protected abstract OrdemDeServicoStatusBase ProximoStatus { get; }
    protected abstract OrdemDeServicoStatusBase StatusRevertido { get; }
    protected abstract OrdemDeServicoStatusBase StatusCancelado { get; }
    public abstract OrdemDeServicoStatus StatusAtual { get; }
    public OrdemDeServicoStatus StatusDestino { get; internal set; }

    public DateTime DataInicio { get; internal set; } = DomainClock.UtcNow;
    public DateTime? DataFinalizacao { get; internal set; }

    public virtual Result<OrdemDeServicoStatusBase> AvancarEtapa(Action<IDomainEvent> addDomainEvent)
    {
        DataFinalizacao = DomainClock.UtcNow;
        var statusFuturo = ProximoStatus;
        StatusDestino = statusFuturo.StatusAtual;
        return Result<OrdemDeServicoStatusBase>.Ok(statusFuturo);
    }

    public virtual Result<OrdemDeServicoStatusBase> ReverterEtapa(Action<IDomainEvent> addDomainEvent)
    {
        DataFinalizacao = DomainClock.UtcNow;
        var statusFuturo = StatusRevertido;
        StatusDestino = statusFuturo.StatusAtual;
        return Result<OrdemDeServicoStatusBase>.Ok(statusFuturo);
    }

    public virtual Result<OrdemDeServicoStatusBase> Cancelar(Action<IDomainEvent> addDomainEvent)
    {
        DataFinalizacao = DomainClock.UtcNow;
        var statusFuturo = StatusCancelado;
        StatusDestino = statusFuturo.StatusAtual;
        return Result<OrdemDeServicoStatusBase>.Ok(statusFuturo);
    }

    /// <summary>
    ///     Rehidrata o estado completo a partir da persistência. Uso restrito ao mapper
    ///     de infraestrutura — não chame em código de domínio/aplicação.
    /// </summary>
    internal void Restaurar(
        Guid id,
        OrdemDeServicoStatus statusDestino,
        DateTime dataInicio,
        DateTime? dataFinalizacao,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        RestaurarBase(id, createdAt, updatedAt);
        StatusDestino = statusDestino;
        DataInicio = dataInicio;
        DataFinalizacao = dataFinalizacao;
    }
}
