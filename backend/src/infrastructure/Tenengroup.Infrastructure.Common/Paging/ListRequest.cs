namespace Tenengroup.Infrastructure.Common.Paging;

public class ListRequest
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 500;
    public const string Descending = "desc";

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;

    public string? Search { get; set; }

    public string? SortBy { get; set; }

    public string? SortDirection { get; set; }

    public Dictionary<string, ListFilter> Filters { get; set; } = new();

    public bool IsDescending => string.Equals(SortDirection, Descending, StringComparison.OrdinalIgnoreCase);

    public ListFilter? Filter(string field)
    {
        return Filters.FirstOrDefault(f => string.Equals(f.Key, field, StringComparison.OrdinalIgnoreCase)).Value;
    }
}
