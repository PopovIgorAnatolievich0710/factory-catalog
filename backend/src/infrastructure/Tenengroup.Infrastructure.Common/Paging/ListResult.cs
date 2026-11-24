namespace Tenengroup.Infrastructure.Common.Paging;

public class ListResult<T>(IReadOnlyCollection<T> items, int totalCount)
{
    public IReadOnlyCollection<T> Items { get; } = items;

    public int TotalCount { get; } = totalCount;
}
