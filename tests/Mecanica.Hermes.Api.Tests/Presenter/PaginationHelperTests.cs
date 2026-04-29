using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Application.Common.Pagination;
using Microsoft.AspNetCore.Http;

namespace Mecanica.Hermes.Api.Tests.Presenter;

[Trait("Category", "Unit")]
public class PaginationHelperTests
{
    private sealed record DomainDto(int Value);
    private sealed record ApiDto(int Value);

    private static HttpRequest BuildRequest(string path = "/api/items", string? query = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = path;
        httpContext.Request.QueryString = query is null ? default : new QueryString(query);
        return httpContext.Request;
    }

    private static ApiDto MapIdentity(DomainDto dto) => new(dto.Value);

    [Fact]
    public void ToPaginatedResponse_DeveMapearMetadataCorretamente()
    {
        var paged = PagedResult<DomainDto>.Create(
            [new DomainDto(1), new DomainDto(2)],
            page: 1, pageSize: 2, totalCount: 5);

        var response = paged.ToPaginatedResponse(MapIdentity, BuildRequest());

        response.Data.Should().HaveCount(2);
        response.Metadata.Page.Should().Be(1);
        response.Metadata.PageSize.Should().Be(2);
        response.Metadata.TotalCount.Should().Be(5);
        response.Metadata.TotalPages.Should().Be(3);
        response.Metadata.HasNextPage.Should().BeTrue();
        response.Metadata.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void ToPaginatedResponse_DeveGerarLinksCompletos()
    {
        var paged = PagedResult<DomainDto>.Create([new DomainDto(1)], page: 2, pageSize: 1, totalCount: 3);

        var response = paged.ToPaginatedResponse(MapIdentity, BuildRequest());

        response.Links.Self.Should().Contain("page=2").And.Contain("pageSize=1");
        response.Links.First.Should().Contain("page=1");
        response.Links.Last.Should().Contain("page=3");
        response.Links.Next.Should().Contain("page=3");
        response.Links.Prev.Should().Contain("page=1");
    }

    [Fact]
    public void ToPaginatedResponse_QuandoVazio_NaoDeveGerarLinkLast()
    {
        var paged = PagedResult<DomainDto>.Create([], page: 1, pageSize: 10, totalCount: 0);

        var response = paged.ToPaginatedResponse(MapIdentity, BuildRequest());

        response.Links.Last.Should().BeNull();
        response.Links.Next.Should().BeNull();
        response.Links.Prev.Should().BeNull();
    }

    [Fact]
    public void ToPaginatedResponse_ComQueryStringExistente_DeveLimparAntesDeRecompor()
    {
        var paged = PagedResult<DomainDto>.Create([new DomainDto(1)], page: 1, pageSize: 10, totalCount: 1);
        var request = BuildRequest("/api/items", "?page=5&pageSize=20");

        var response = paged.ToPaginatedResponse(MapIdentity, request);

        response.Links.Self.Should().NotContain("page=5")
            .And.Contain("page=1")
            .And.Contain("pageSize=10");
    }
}
