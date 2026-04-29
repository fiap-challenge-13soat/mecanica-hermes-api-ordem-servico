using Mecanica.Hermes.Domain.Common.Abstractions;
using Mecanica.Hermes.Domain.Common.Results;
using Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.Domain.OrdensDeServico.Status;

namespace Mecanica.Hermes.Domain.OrdensDeServico;

public class OrdemDeServico : AggregateRoot
{
    private readonly List<OrdemDeServicoHistoricoStatus> _historicosStatus;
    private readonly List<OrdemDeServicoProduto> _produtos;
    private readonly List<OrdemDeServicoServico> _servicos;

    private OrdemDeServico(
        Guid id,
        Guid clienteId,
        Guid veiculoId,
        string? problemaRelatado,
        string? observacoes,
        OrdemDeServicoStatusBase statusAtual,
        IEnumerable<OrdemDeServicoHistoricoStatus>? historicosStatus = null,
        IEnumerable<OrdemDeServicoProduto>? produtos = null,
        IEnumerable<OrdemDeServicoServico>? servicos = null)
        : base(id)
    {
        ClienteId = clienteId;
        VeiculoId = veiculoId;
        ProblemaRelatado = problemaRelatado;
        Observacoes = observacoes;
        StatusAtual = statusAtual;
        _historicosStatus = historicosStatus?.ToList() ?? [];
        _produtos = produtos?.ToList() ?? [];
        _servicos = servicos?.ToList() ?? [];
    }

    public IReadOnlyCollection<OrdemDeServicoHistoricoStatus> HistoricosStatus => _historicosStatus;
    public OrdemDeServicoStatusBase StatusAtual { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid VeiculoId { get; private set; }
    public string? ProblemaRelatado { get; private set; }
    public string? Observacoes { get; private set; }
    public IReadOnlyCollection<OrdemDeServicoProduto> Produtos => _produtos;
    public IReadOnlyCollection<OrdemDeServicoServico> Servicos => _servicos;

    public decimal ValorTotal => _produtos.Sum(p => p.ValorVenda.Valor * p.Quantidade.Valor) +
                                 _servicos.Sum(s => s.ValorVenda.Valor);

    public static Result<OrdemDeServico> Criar(
        Guid id,
        Guid clienteId,
        Guid veiculoId,
        string? problemaRelatado,
        string? observacoes)
    {
        if (id == Guid.Empty)
            return Result<OrdemDeServico>.BadRequest("O Id da ordem de serviço deve ser informado.");

        var statusInicial = new OrdemDeServicoRecebida();
        var ordemServico = new OrdemDeServico(id, clienteId, veiculoId, problemaRelatado, observacoes, statusInicial);

        ordemServico.AddDomainEvent(new OrdemDeServicoCriadoEvent(
            ordemServico.Id,
            clienteId,
            veiculoId,
            problemaRelatado,
            statusInicial.StatusAtual));

        return Result<OrdemDeServico>.Ok(ordemServico);
    }

    internal static OrdemDeServico Restaurar(
        Guid id,
        Guid clienteId,
        Guid veiculoId,
        string? problemaRelatado,
        string? observacoes,
        OrdemDeServicoStatusBase statusAtual,
        IEnumerable<OrdemDeServicoHistoricoStatus>? historicoStatus,
        IEnumerable<OrdemDeServicoProduto>? produtos,
        IEnumerable<OrdemDeServicoServico>? servicos,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        var ordemServico = new OrdemDeServico(
            id,
            clienteId,
            veiculoId,
            problemaRelatado,
            observacoes,
            statusAtual,
            historicoStatus,
            produtos,
            servicos);

        ordemServico.RestaurarBase(id, createdAt, updatedAt);
        return ordemServico;
    }

    public Result<OrdemDeServicoProduto> AdicionarProduto(Guid produtoId, decimal valorVenda, int quantidade)
    {
        if (!StatusAtual.PermiteEditarProdutos)
            return Result<OrdemDeServicoProduto>.BadRequest("O status atual da ordem de serviço não permite edição de produtos.");

        if (produtoId == Guid.Empty)
            return Result<OrdemDeServicoProduto>.BadRequest("O produtoId deve ser informado.");

        if (quantidade <= 0)
            return Result<OrdemDeServicoProduto>.BadRequest("A quantidade deve ser maior que zero.");

        if (Produtos.Any(p => p.ProdutoId == produtoId))
            return Result<OrdemDeServicoProduto>.BadRequest("O produto já foi adicionado à ordem de serviço.");

        var ordemDeServicoProduto = OrdemDeServicoProduto.Criar(produtoId, valorVenda, quantidade);
        if (ordemDeServicoProduto.IsFailure)
            return Result<OrdemDeServicoProduto>.BadRequest(ordemDeServicoProduto.Errors);

        _produtos.Add(ordemDeServicoProduto.Data!);

        MarkAsUpdated();

        return Result<OrdemDeServicoProduto>.Ok(ordemDeServicoProduto.Data);
    }

    public Result<OrdemDeServicoProduto> RemoverProduto(Guid produtoId)
    {
        if (!StatusAtual.PermiteEditarProdutos)
            return Result<OrdemDeServicoProduto>.BadRequest("O status atual da ordem de serviço não permite edição de produtos.");

        var produto = Produtos.FirstOrDefault(p => p.ProdutoId == produtoId);
        if (produto == null)
            return Result<OrdemDeServicoProduto>.BadRequest("Produto não encontrado.");

        _produtos.Remove(produto);

        MarkAsUpdated();

        return Result<OrdemDeServicoProduto>.Ok();
    }

    public Result<OrdemDeServicoServico> AdicionarServico(Guid servicoId, decimal valorVenda)
    {
        if (!StatusAtual.PermiteEditarProdutos)
            return Result<OrdemDeServicoServico>.BadRequest("O status atual da ordem de serviço não permite edição de serviços.");

        if (Servicos.Any(s => s.ServicoId == servicoId))
            return Result<OrdemDeServicoServico>.BadRequest("O serviço já foi adicionado à ordem de serviço.");

        var ordemDeServicoServico = OrdemDeServicoServico.Criar(servicoId, valorVenda);
        if (ordemDeServicoServico.IsFailure)
            return Result<OrdemDeServicoServico>.BadRequest(ordemDeServicoServico.Errors);

        _servicos.Add(ordemDeServicoServico.Data!);

        MarkAsUpdated();

        return Result<OrdemDeServicoServico>.Ok(ordemDeServicoServico.Data);
    }

    public Result<OrdemDeServicoServico> RemoverServico(Guid servicoId)
    {
        if (!StatusAtual.PermiteEditarProdutos)
            return Result<OrdemDeServicoServico>.BadRequest("O status atual da ordem de serviço não permite edição de serviços.");

        var servico = Servicos.FirstOrDefault(s => s.ServicoId == servicoId);

        if (servico == null)
            return Result<OrdemDeServicoServico>.BadRequest("Serviço não encontrado.");

        _servicos.Remove(servico);

        MarkAsUpdated();

        return Result<OrdemDeServicoServico>.Ok();
    }

    public Result AvancarEtapa()
    {
        var statusAnterior = StatusAtual;
        var novoStatus = StatusAtual.AvancarEtapa(AddDomainEvent);
        if (novoStatus.IsFailure)
            return Result.BadRequest(novoStatus.Errors);

        StatusAtual = novoStatus.Data!;
        _historicosStatus.Add(new OrdemDeServicoHistoricoStatus(statusAnterior, ResolverStatusAnteriorParaHistorico(statusAnterior)));
        var duracaoEtapa = statusAnterior.DataFinalizacao.HasValue
            ? (statusAnterior.DataFinalizacao.Value - statusAnterior.DataInicio).TotalMinutes
            : (double?)null;
        AddDomainEvent(new OrdemDeServicoEtapaAvancadaEvent(Id, statusAnterior.StatusAtual, novoStatus.Data!.StatusAtual, duracaoEtapa));
        
        MarkAsUpdated();
        
        return Result.Ok();
    }

    public Result ReverterEtapa()
    {
        var statusAnterior = StatusAtual;
        var novoStatus = StatusAtual.ReverterEtapa(AddDomainEvent);
        if (novoStatus.IsFailure)
            return Result.BadRequest(novoStatus.Errors);

        StatusAtual = novoStatus.Data!;
        _historicosStatus.Add(new OrdemDeServicoHistoricoStatus(statusAnterior, ResolverStatusAnteriorParaHistorico(statusAnterior)));
        var duracaoEtapa = statusAnterior.DataFinalizacao.HasValue
            ? (statusAnterior.DataFinalizacao.Value - statusAnterior.DataInicio).TotalMinutes
            : (double?)null;
        AddDomainEvent(new OrdemDeServicoEtapaRevertidaEvent(Id, statusAnterior.StatusAtual, novoStatus.Data!.StatusAtual, duracaoEtapa));
        
        MarkAsUpdated();

        return Result.Ok();
    }

    public Result Cancelar()
    {
        var statusAnterior = StatusAtual;
        var novoStatus = StatusAtual.Cancelar(AddDomainEvent);
        if (novoStatus.IsFailure)
            return Result.BadRequest(novoStatus.Errors);

        StatusAtual = novoStatus.Data!;
        _historicosStatus.Add(new OrdemDeServicoHistoricoStatus(statusAnterior, ResolverStatusAnteriorParaHistorico(statusAnterior)));
        AddDomainEvent(new OrdemDeServicoCanceladaEvent(Id, statusAnterior.StatusAtual));
        
        MarkAsUpdated();

        return Result.Ok();
    }

    /// <summary>
    /// Calcula o <c>StatusAnterior</c> a ser persistido no item de histórico que está
    /// sendo arquivado, olhando o histórico já existente. Para o primeiro item registrado
    /// (ainda não há histórico), retorna o próprio status sob transição — semântica de
    /// "sem predecessor".
    /// </summary>
    private OrdemDeServicoStatus ResolverStatusAnteriorParaHistorico(OrdemDeServicoStatusBase statusSendoArquivado)
    {
        if (_historicosStatus.Count == 0)
            return statusSendoArquivado.StatusAtual;

        return _historicosStatus[^1].StatusAtual;
    }
}