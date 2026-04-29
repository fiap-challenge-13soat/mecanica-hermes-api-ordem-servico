using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Domain.OrdensDeServico.Events;
using Mecanica.Hermes.TestUtils.Builders;

namespace Mecanica.Hermes.Domain.Tests.OrdensDeServico;

/// <summary>
/// Testes do agregado raiz <see cref="OrdemDeServico"/>. Organizados por feature
/// (criar, produtos, serviços, ciclo de vida) seguindo AAA (Arrange-Act-Assert).
/// </summary>
[Trait("Category", "Unit")]
public class OrdemDeServicoTests
{
    // ---------------------------------------------------------------------
    // Criar
    // ---------------------------------------------------------------------

    [Fact]
    public void Criar_DeveRetornarSucesso_QuandoDadosValidos()
    {
        var id = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var result = OrdemDeServico.Criar(id, clienteId, veiculoId, "Motor", "Obs");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(id);
        result.Data.ClienteId.Should().Be(clienteId);
        result.Data.VeiculoId.Should().Be(veiculoId);
        result.Data.ProblemaRelatado.Should().Be("Motor");
        result.Data.Observacoes.Should().Be("Obs");
        result.Data.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.Recebida);
    }

    [Fact]
    public void Criar_DeveRetornarErro_QuandoIdVazio()
    {
        var result = OrdemDeServico.Criar(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), null, null);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("O Id da ordem de serviço deve ser informado"));
    }

    [Fact]
    public void Criar_DeveEmitirOrdemDeServicoCriadoEvent()
    {
        var result = OrdemDeServico.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x", null);

        result.Data!.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrdemDeServicoCriadoEvent>();
    }

    // ---------------------------------------------------------------------
    // Produtos
    // ---------------------------------------------------------------------

    [Fact]
    public void AdicionarProduto_DeveRetornarSucesso_QuandoStatusPermiteEdicao()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        var produtoId = Guid.NewGuid();

        var result = ordem.AdicionarProduto(produtoId, 50m, 2);

        result.IsSuccess.Should().BeTrue();
        ordem.Produtos.Should().ContainSingle(p => p.ProdutoId == produtoId);
    }

    [Fact]
    public void AdicionarProduto_DeveRetornarErro_QuandoProdutoJaAdicionado()
    {
        var produtoId = Guid.NewGuid();
        var ordem = new OrdemDeServicoBuilder().ComProduto(produtoId, 50m, 1).Build();

        var result = ordem.AdicionarProduto(produtoId, 50m, 1);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("já foi adicionado"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AdicionarProduto_DeveRetornarErro_QuandoQuantidadeInvalida(int quantidade)
    {
        var ordem = new OrdemDeServicoBuilder().Build();

        var result = ordem.AdicionarProduto(Guid.NewGuid(), 50m, quantidade);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("quantidade deve ser maior que zero"));
    }

    [Fact]
    public void AdicionarProduto_DeveRetornarErro_QuandoProdutoIdVazio()
    {
        var ordem = new OrdemDeServicoBuilder().Build();

        var result = ordem.AdicionarProduto(Guid.Empty, 50m, 1);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("produtoId deve ser informado"));
    }

    [Fact]
    public void AdicionarProduto_DeveRetornarErro_QuandoStatusNaoPermiteEdicao()
    {
        // Após AguardandoAprovacao (2 avanços a partir de Recebida), produtos não podem ser editados
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(2).Build();

        var result = ordem.AdicionarProduto(Guid.NewGuid(), 50m, 1);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoverProduto_DeveRetornarSucesso_QuandoProdutoExiste()
    {
        var produtoId = Guid.NewGuid();
        var ordem = new OrdemDeServicoBuilder().ComProduto(produtoId).Build();

        var result = ordem.RemoverProduto(produtoId);

        result.IsSuccess.Should().BeTrue();
        ordem.Produtos.Should().BeEmpty();
    }

    [Fact]
    public void RemoverProduto_DeveRetornarErro_QuandoProdutoNaoExiste()
    {
        var ordem = new OrdemDeServicoBuilder().Build();

        var result = ordem.RemoverProduto(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Produto não encontrado"));
    }

    // ---------------------------------------------------------------------
    // Serviços
    // ---------------------------------------------------------------------

    [Fact]
    public void AdicionarServico_DeveRetornarSucesso_QuandoStatusPermiteEdicao()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        var servicoId = Guid.NewGuid();

        var result = ordem.AdicionarServico(servicoId, 100m);

        result.IsSuccess.Should().BeTrue();
        ordem.Servicos.Should().ContainSingle(s => s.ServicoId == servicoId);
    }

    [Fact]
    public void AdicionarServico_DeveRetornarErro_QuandoServicoJaAdicionado()
    {
        var servicoId = Guid.NewGuid();
        var ordem = new OrdemDeServicoBuilder().ComServico(servicoId).Build();

        var result = ordem.AdicionarServico(servicoId, 100m);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("já foi adicionado"));
    }

    [Fact]
    public void AdicionarServico_DeveRetornarErro_QuandoStatusNaoPermiteEdicao()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(2).Build();

        var result = ordem.AdicionarServico(Guid.NewGuid(), 100m);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoverServico_DeveRetornarSucesso_QuandoServicoExiste()
    {
        var servicoId = Guid.NewGuid();
        var ordem = new OrdemDeServicoBuilder().ComServico(servicoId).Build();

        var result = ordem.RemoverServico(servicoId);

        result.IsSuccess.Should().BeTrue();
        ordem.Servicos.Should().BeEmpty();
    }

    [Fact]
    public void RemoverServico_DeveRetornarErro_QuandoServicoNaoExiste()
    {
        var ordem = new OrdemDeServicoBuilder().Build();

        var result = ordem.RemoverServico(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Serviço não encontrado"));
    }

    // ---------------------------------------------------------------------
    // ValorTotal
    // ---------------------------------------------------------------------

    [Fact]
    public void ValorTotal_DeveSomarProdutosEServicos()
    {
        var ordem = new OrdemDeServicoBuilder()
            .ComProduto(valor: 50m, quantidade: 3)   // 150
            .ComServico(valor: 100m)                 // 100
            .Build();

        ordem.ValorTotal.Should().Be(250m);
    }

    [Fact]
    public void ValorTotal_DeveExcluirProdutosRemovidos()
    {
        var produtoId = Guid.NewGuid();
        var ordem = new OrdemDeServicoBuilder()
            .ComProduto(produtoId, 50m, 2)
            .ComServico(valor: 100m)
            .Build();

        ordem.RemoverProduto(produtoId);

        ordem.ValorTotal.Should().Be(100m);
    }

    // ---------------------------------------------------------------------
    // Ciclo de vida (Avançar / Reverter / Cancelar)
    // ---------------------------------------------------------------------

    [Fact]
    public void AvancarEtapa_DeveMoverStatusEAdicionarHistorico()
    {
        var ordem = new OrdemDeServicoBuilder().Build();

        var result = ordem.AvancarEtapa();

        result.IsSuccess.Should().BeTrue();
        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.EmDiagnostico);
        ordem.HistoricosStatus.Should().ContainSingle();
        ordem.DomainEvents.Should().ContainSingle(e => e is OrdemDeServicoEtapaAvancadaEvent);
    }

    [Fact]
    public void AvancarEtapa_DeveRetornarErro_QuandoStatusNaoPermiteAvanco()
    {
        var ordem = new OrdemDeServicoBuilder().Cancelada().Build();

        var result = ordem.AvancarEtapa();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ReverterEtapa_DeveVoltarStatusEAdicionarHistorico()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(1).Build();
        var statusAtual = ordem.StatusAtual.StatusAtual;

        var result = ordem.ReverterEtapa();

        result.IsSuccess.Should().BeTrue();
        ordem.StatusAtual.StatusAtual.Should().NotBe(statusAtual);
        ordem.DomainEvents.Should().ContainSingle(e => e is OrdemDeServicoEtapaRevertidaEvent);
    }

    [Fact]
    public void ReverterEtapa_DeveRetornarErro_QuandoStatusNaoPermiteReversao()
    {
        var ordem = new OrdemDeServicoBuilder().Build();

        var result = ordem.ReverterEtapa();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("não pode rever"));
    }

    [Fact]
    public void Cancelar_DeveMoverParaCanceladaEEmitirEvento()
    {
        var ordem = new OrdemDeServicoBuilder().Build();

        var result = ordem.Cancelar();

        result.IsSuccess.Should().BeTrue();
        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.Cancelada);
        ordem.DomainEvents.Should().ContainSingle(e => e is OrdemDeServicoCanceladaEvent);
    }

    [Fact]
    public void Cancelar_DeveRetornarErro_QuandoJaCancelada()
    {
        var ordem = new OrdemDeServicoBuilder().Cancelada().Build();

        var result = ordem.Cancelar();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AvancarMultiplasVezes_DeveAcumularHistorico()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(3).Build();

        ordem.HistoricosStatus.Should().HaveCount(3);
    }

    // ---------------------------------------------------------------------
    // Encapsulamento (regressão da Fase 1 do refactor arquitetural)
    // ---------------------------------------------------------------------

    [Fact]
    public void Produtos_DeveRetornarReadOnlyCollection()
    {
        var ordem = new OrdemDeServicoBuilder().ComProduto().Build();

        ordem.Produtos.Should().BeAssignableTo<IReadOnlyCollection<OrdemDeServicoProduto>>();
    }

    [Fact]
    public void Servicos_DeveRetornarReadOnlyCollection()
    {
        var ordem = new OrdemDeServicoBuilder().ComServico().Build();

        ordem.Servicos.Should().BeAssignableTo<IReadOnlyCollection<OrdemDeServicoServico>>();
    }
}
