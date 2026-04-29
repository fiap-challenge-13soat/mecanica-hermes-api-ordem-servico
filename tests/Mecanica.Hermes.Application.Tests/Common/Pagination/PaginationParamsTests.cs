using Mecanica.Hermes.Application.Common.Pagination;

namespace Mecanica.Hermes.Application.Tests.Common.Pagination;

[Trait("Category", "Unit")]
public class PaginationParamsTests
{
    [Fact]
    public void DefaultValues_DeveSerPage1PageSize10()
    {
        var p = new PaginationParams();

        p.Page.Should().Be(1);
        p.PageSize.Should().Be(10);
        p.Skip.Should().Be(0);
    }

    [Fact]
    public void Skip_DeveCalcularOffsetCorretamente()
    {
        var p = new PaginationParams { Page = 3, PageSize = 20 };

        p.Skip.Should().Be(40);
    }

    [Fact]
    public void PageSize_AcimaDoMaximo_DeveClipar()
    {
        var p = new PaginationParams { PageSize = 9999 };

        p.PageSize.Should().Be(100);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageSize_InvalidoOuZero_DeveAplicarDefault(int value)
    {
        var p = new PaginationParams { PageSize = value };

        p.PageSize.Should().Be(10);
    }
}
