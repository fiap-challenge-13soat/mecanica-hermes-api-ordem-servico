using Bogus;
using Mecanica.Hermes.Domain.OrdensDeServico;

namespace Mecanica.Hermes.TestUtils.Builders;

/// <summary>
/// Test Data Builder para <see cref="OrdemDeServico"/>.
/// Usa <c>Bogus</c> para gerar dados realistas; permite override fluente de campos
/// específicos por teste. Padrão recomendado em xUnit + DDD para evitar
/// <c>Guid.NewGuid()</c> espalhado.
///
/// Exemplo:
/// <code>
/// var ordem = new OrdemDeServicoBuilder()
///     .ComProblemaRelatado("Motor")
///     .ComProduto(produtoId: id, valor: 100m, quantidade: 2)
///     .Build();
/// </code>
/// </summary>
public sealed class OrdemDeServicoBuilder
{
    private static readonly Faker Faker = new("pt_BR");

    private Guid _id = Guid.NewGuid();
    private Guid _clienteId = Guid.NewGuid();
    private Guid _veiculoId = Guid.NewGuid();
    private string? _problemaRelatado = Faker.Lorem.Sentence();
    private string? _observacoes = Faker.Lorem.Sentence();
    private readonly List<(Guid produtoId, decimal valor, int quantidade)> _produtos = [];
    private readonly List<(Guid servicoId, decimal valor)> _servicos = [];
    private int _avancarVezes;
    private bool _cancelar;

    public OrdemDeServicoBuilder ComId(Guid id)
    {
        _id = id;
        return this;
    }

    public OrdemDeServicoBuilder ComCliente(Guid clienteId)
    {
        _clienteId = clienteId;
        return this;
    }

    public OrdemDeServicoBuilder ComVeiculo(Guid veiculoId)
    {
        _veiculoId = veiculoId;
        return this;
    }

    public OrdemDeServicoBuilder ComProblemaRelatado(string? problema)
    {
        _problemaRelatado = problema;
        return this;
    }

    public OrdemDeServicoBuilder ComObservacoes(string? observacoes)
    {
        _observacoes = observacoes;
        return this;
    }

    public OrdemDeServicoBuilder ComProduto(Guid? produtoId = null, decimal valor = 100m, int quantidade = 1)
    {
        _produtos.Add((produtoId ?? Guid.NewGuid(), valor, quantidade));
        return this;
    }

    public OrdemDeServicoBuilder ComServico(Guid? servicoId = null, decimal valor = 150m)
    {
        _servicos.Add((servicoId ?? Guid.NewGuid(), valor));
        return this;
    }

    /// <summary>
    /// Avança o status N vezes a partir de <c>Recebida</c>. Atalho para preparar a OS
    /// em um estado específico do ciclo de vida.
    /// </summary>
    public OrdemDeServicoBuilder ComStatusAvancadoEm(int vezes)
    {
        _avancarVezes = vezes;
        return this;
    }

    public OrdemDeServicoBuilder Cancelada()
    {
        _cancelar = true;
        return this;
    }

    public OrdemDeServico Build()
    {
        var ordem = OrdemDeServico.Criar(_id, _clienteId, _veiculoId, _problemaRelatado, _observacoes).Data!;

        foreach (var (produtoId, valor, quantidade) in _produtos)
            ordem.AdicionarProduto(produtoId, valor, quantidade);

        foreach (var (servicoId, valor) in _servicos)
            ordem.AdicionarServico(servicoId, valor);

        for (var i = 0; i < _avancarVezes; i++)
            ordem.AvancarEtapa();

        if (_cancelar)
            ordem.Cancelar();

        ordem.ClearDomainEvents();
        return ordem;
    }
}
