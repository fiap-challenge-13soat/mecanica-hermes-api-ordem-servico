using Mecanica.Hermes.Domain.OrdensDeServico.ValueObjects;

namespace Mecanica.Hermes.Domain.Tests.OrdensDeServico.ValueObjects;

[Trait("Category", "Unit")]
public class ValueObjectsTests
{
    // ---- QuantidadeVo ----

    [Fact]
    public void QuantidadeVo_Criar_ComValorPositivo_DeveRetornarSucesso()
    {
        var result = QuantidadeVo.Criar(5);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Valor.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void QuantidadeVo_Criar_ComValorInvalido_DeveRetornarErro(int valor)
    {
        var result = QuantidadeVo.Criar(valor);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("positiva"));
    }

    [Fact]
    public void QuantidadeVo_ToString_DeveRetornarValorComoString()
    {
        new QuantidadeVo(42).ToString().Should().Be("42");
    }

    // ---- ValorVendaVo ----

    [Fact]
    public void ValorVendaVo_Criar_ComValorPositivo_DeveRetornarSucesso()
    {
        var result = ValorVendaVo.Criar(10.50m);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Valor.Should().Be(10.50m);
    }

    [Fact]
    public void ValorVendaVo_Criar_ComZero_DeveRetornarSucesso()
    {
        var result = ValorVendaVo.Criar(0m);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Valor.Should().Be(0m);
    }

    [Fact]
    public void ValorVendaVo_Criar_ComNegativo_DeveRetornarErro()
    {
        var result = ValorVendaVo.Criar(-1m);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("negativo"));
    }

    [Theory]
    [InlineData(10.999, 10.99)]
    [InlineData(10.005, 10.00)]
    [InlineData(10.00, 10.00)]
    public void ValorVendaVo_Criar_DeveTruncarPara2CasasDecimais(decimal input, decimal expected)
    {
        var result = ValorVendaVo.Criar(input);

        result.Data!.Valor.Should().Be(expected);
    }
}
