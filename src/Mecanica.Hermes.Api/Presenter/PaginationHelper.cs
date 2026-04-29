using Mecanica.Hermes.Application.Common.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Mecanica.Hermes.Api.Presenter;

public static class PaginationHelper
{
    public static PaginatedResponse<TApi> ToPaginatedResponse<TApp, TApi>(
        this PagedResult<TApp> pagedResult,
        Func<TApp, TApi> map,
        HttpRequest request)
    {
        var items = pagedResult.Items.Select(map);
        var baseUrl = GetBaseUrl(request);

        return new PaginatedResponse<TApi>
        {
            Data = items,
            Links = CreateLinks(baseUrl, pagedResult),
            Metadata = new PaginationMetadata
            {
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount,
                TotalPages = pagedResult.TotalPages,
                HasPreviousPage = pagedResult.HasPreviousPage,
                HasNextPage = pagedResult.HasNextPage
            }
        };
    }

    private static string GetBaseUrl(HttpRequest request)
    {
        var queryString = request.QueryString.Value ?? string.Empty;
        var uri = request.GetEncodedUrl();

        if (!string.IsNullOrEmpty(queryString))
        {
            uri = uri.Replace(queryString, string.Empty);
        }

        return uri.TrimEnd('?');
    }

    private static PaginationLinks CreateLinks<T>(string baseUrl, PagedResult<T> pagedResult)
    {
        var links = new PaginationLinks
        {
            Self = BuildUrl(baseUrl, pagedResult.Page, pagedResult.PageSize),
            First = BuildUrl(baseUrl, 1, pagedResult.PageSize),
            Last = pagedResult.TotalPages > 0 
                ? BuildUrl(baseUrl, pagedResult.TotalPages, pagedResult.PageSize) 
                : null
        };

        if (pagedResult.HasNextPage)
        {
            links = links with { Next = BuildUrl(baseUrl, pagedResult.Page + 1, pagedResult.PageSize) };
        }

        if (pagedResult.HasPreviousPage)
        {
            links = links with { Prev = BuildUrl(baseUrl, pagedResult.Page - 1, pagedResult.PageSize) };
        }

        return links;
    }

    private static string BuildUrl(string baseUrl, int page, int pageSize)
    {
        var separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}page={page}&pageSize={pageSize}";
    }
}
