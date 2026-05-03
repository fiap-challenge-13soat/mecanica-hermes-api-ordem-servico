namespace Mecanica.Hermes.Api.Presenter;

public sealed class PaginatedResponse<T>
{
    public IEnumerable<T> Data { get; init; } = [];
    public PaginationLinks Links { get; init; } = new();
    public PaginationMetadata Metadata { get; init; } = new();
}

public sealed record PaginationLinks
{
    public string Self { get; init; } = string.Empty;
    public string? Next { get; init; }
    public string? Prev { get; init; }
    public string? First { get; init; }
    public string? Last { get; init; }
}

public sealed class PaginationMetadata
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}
