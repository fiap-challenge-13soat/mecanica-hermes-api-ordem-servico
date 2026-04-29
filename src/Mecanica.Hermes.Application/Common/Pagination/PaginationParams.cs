namespace Mecanica.Hermes.Application.Common.Pagination;

public sealed class PaginationParams
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 10;

    public int Page { get; init; } = 1;
    private int _pageSize = DefaultPageSize;

    public int PageSize
    {
        get => _pageSize;
        init
        {
            if (value > MaxPageSize)
            {
                _pageSize = MaxPageSize;
            }
            else if (value < 1)
            {
                _pageSize = DefaultPageSize;
            }
            else
            {
                _pageSize = value;
            }
        }
    }

    public int Skip => (Page - 1) * PageSize;
}
