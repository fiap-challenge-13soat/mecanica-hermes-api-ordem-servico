using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Infrastructure.Persistence.Mappings;
using Mecanica.Hermes.TestUtils.Builders;

namespace Mecanica.Hermes.Infrastructure.Tests.Persistence.Mappings;

/// <summary>
///     Testes do mapper de persistência. Validam o round-trip Domain ↔ Entity preservando
///     os invariantes do agregado (Id, timestamps, status atual, coleções).
/// </summary>
[Trait("Category", "Unit")]
public class OrdemDeServicoMapperTests
{
    [Fact]
    public void ToEntity_DeveCopiarCamposBasicos()
    {
        var produtoId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        var ordem = new OrdemDeServicoBuilder()
            .ComProblemaRelatado("Motor")
            .ComObservacoes("Obs")
            .ComProduto(produtoId, 100m, 2)
            .ComServico(servicoId)
            .Build();

        var entity = ordem.ToEntity();

        entity.Id.Should().Be(ordem.Id);
        entity.ClienteId.Should().Be(ordem.ClienteId);
        entity.VeiculoId.Should().Be(ordem.VeiculoId);
        entity.ProblemaRelatado.Should().Be("Motor");
        entity.Observacoes.Should().Be("Obs");
        entity.Produtos.Should().ContainSingle(p => p.ProdutoId == produtoId && p.Quantidade.Valor == 2);
        entity.Servicos.Should().ContainSingle(s => s.ServicoId == servicoId);
        entity.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.Recebida);
    }

    [Fact]
    public void ToEntity_ThenToDomain_DevePreservarEstado()
    {
        var ordem = new OrdemDeServicoBuilder()
            .ComProblemaRelatado("Problema")
            .ComObservacoes("Notas")
            .ComProduto(valor: 200m, quantidade: 3)
            .ComServico(valor: 50m)
            .ComStatusAvancadoEm(1)
            .Build();

        var rehydrated = ordem.ToEntity().ToDomain();

        rehydrated.Id.Should().Be(ordem.Id);
        rehydrated.ClienteId.Should().Be(ordem.ClienteId);
        rehydrated.VeiculoId.Should().Be(ordem.VeiculoId);
        rehydrated.ProblemaRelatado.Should().Be(ordem.ProblemaRelatado);
        rehydrated.Observacoes.Should().Be(ordem.Observacoes);
        rehydrated.StatusAtual.StatusAtual.Should().Be(ordem.StatusAtual.StatusAtual);
        rehydrated.Produtos.Should().HaveCount(ordem.Produtos.Count);
        rehydrated.Servicos.Should().HaveCount(ordem.Servicos.Count);
        rehydrated.HistoricosStatus.Should().HaveCount(ordem.HistoricosStatus.Count);
        rehydrated.ValorTotal.Should().Be(ordem.ValorTotal);
    }

    [Fact]
    public void ToDomain_DevePreservarDataCriacaoEAtualizacao()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(1).Build();

        var rehydrated = ordem.ToEntity().ToDomain();

        rehydrated.CreatedAt.Should().Be(ordem.CreatedAt);
        rehydrated.UpdatedAt.Should().Be(ordem.UpdatedAt);
    }

    [Theory]
    [InlineData(0, OrdemDeServicoStatus.Recebida)]
    [InlineData(1, OrdemDeServicoStatus.EmDiagnostico)]
    [InlineData(2, OrdemDeServicoStatus.AguardandoAprovacao)]
    public void ToEntity_ThenToDomain_DeveMapearStatusAtualCorretamente(
        int avancos, OrdemDeServicoStatus expectedStatus)
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(avancos).Build();

        var rehydrated = ordem.ToEntity().ToDomain();

        rehydrated.StatusAtual.StatusAtual.Should().Be(expectedStatus);
    }
}
