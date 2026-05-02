using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.TestUtils.Builders;

namespace Mecanica.Hermes.Domain.Tests.OrdensDeServico.Status;

/// <summary>
///     Testes do ciclo de vida completo percorrendo todos os estados concretos:
///     Recebida → EmDiagnostico → AguardandoAprovacao → EmExecucao → ManutencaoFinalizada
///     → AguardandoPagamento → PagamentoConfirmado → Entregue.
///     Cobre as implementações dos estados individuais sem depender de testes triviais
///     (cada estado é testado implicitamente ao avançar).
/// </summary>
[Trait("Category", "Unit")]
public class OrdemDeServicoStatusLifecycleTests
{
    [Theory]
    [InlineData(0, OrdemDeServicoStatus.Recebida)]
    [InlineData(1, OrdemDeServicoStatus.EmDiagnostico)]
    [InlineData(2, OrdemDeServicoStatus.AguardandoAprovacao)]
    [InlineData(3, OrdemDeServicoStatus.EmExecucao)]
    [InlineData(4, OrdemDeServicoStatus.ManutencaoFinalizada)]
    [InlineData(5, OrdemDeServicoStatus.AguardandoPagamento)]
    [InlineData(6, OrdemDeServicoStatus.PagamentoConfirmado)]
    [InlineData(7, OrdemDeServicoStatus.Entregue)]
    public void AvancarEtapa_DeveSeguirCicloDeVidaCompleto(int avancos, OrdemDeServicoStatus expectedStatus)
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(avancos).Build();

        ordem.StatusAtual.StatusAtual.Should().Be(expectedStatus);
    }

    [Fact]
    public void AvancarEtapa_APartirDeEntregue_DeveRetornarErro()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(7).Build();

        var result = ordem.AvancarEtapa();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ReverterEtapa_EmCancelada_DeveRetornarErro()
    {
        var ordem = new OrdemDeServicoBuilder().Cancelada().Build();

        var result = ordem.ReverterEtapa();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("cancelada"));
    }

    [Fact]
    public void Cancelar_EmAguardandoAprovacao_DeveMoverParaRejeitada()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(2).Build();

        var result = ordem.Cancelar();

        result.IsSuccess.Should().BeTrue();
        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.Rejeitada);
    }

    [Fact]
    public void AvancarEtapa_EmRejeitada_DeveRetornarErro()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(2).Build();
        ordem.Cancelar(); // → Rejeitada

        var result = ordem.AvancarEtapa();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("rejeitada"));
    }

    [Fact]
    public void ReverterEtapa_EmRejeitada_DeveVoltarParaAguardandoAprovacao()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(2).Build();
        ordem.Cancelar(); // → Rejeitada

        var result = ordem.ReverterEtapa();

        result.IsSuccess.Should().BeTrue();
        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.AguardandoAprovacao);
    }

    [Fact]
    public void Cancelar_EmEntregue_DeveMoverParaCancelada()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(7).Build();

        var result = ordem.Cancelar();

        result.IsSuccess.Should().BeTrue();
        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.Cancelada);
    }

    /// <summary>
    ///     Regressão do bug onde <c>OrdemDeServicoHistoricoStatus.StatusAnterior</c> nunca era setado,
    ///     gerando histórico inconsistente para auditoria.
    /// </summary>
    [Fact]
    public void HistoricoStatus_PrimeiroItem_DeveTerStatusAnteriorIgualAoPropio()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(1).Build();

        var primeiro = ordem.HistoricosStatus.Should().ContainSingle().Subject;
        primeiro.StatusAtual.Should().Be(OrdemDeServicoStatus.Recebida);
        primeiro.StatusAnterior.Should().Be(OrdemDeServicoStatus.Recebida,
            "primeiro histórico não tem predecessor; convenção é manter o próprio");
    }

    [Fact]
    public void HistoricoStatus_ItensSubsequentes_DeveTerStatusAnteriorComoStatusAtualDoItemAnterior()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(3).Build();

        var historicos = ordem.HistoricosStatus.ToList();
        historicos.Should().HaveCount(3);

        historicos[0].StatusAtual.Should().Be(OrdemDeServicoStatus.Recebida);
        historicos[0].StatusAnterior.Should().Be(OrdemDeServicoStatus.Recebida);

        historicos[1].StatusAtual.Should().Be(OrdemDeServicoStatus.EmDiagnostico);
        historicos[1].StatusAnterior.Should().Be(OrdemDeServicoStatus.Recebida);

        historicos[2].StatusAtual.Should().Be(OrdemDeServicoStatus.AguardandoAprovacao);
        historicos[2].StatusAnterior.Should().Be(OrdemDeServicoStatus.EmDiagnostico);
    }
}
