using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Tenengroup.Infrastructure.Common.Paging;

public static class QueryableListExtensions
{
    public static IQueryable<T> ApplyFilter<T, TValue>(
        this IQueryable<T> query,
        ListRequest request,
        string field,
        Expression<Func<T, TValue>> selector)
    {
        var filter = request.Filter(field);
        var predicate = filter == null ? null : ListFilterExpression.Build(field, filter, selector);

        return predicate == null ? query : query.Where(predicate);
    }

    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        ListRequest request,
        IReadOnlyDictionary<string, Expression<Func<T, object?>>> sortableFields,
        string defaultSortBy)
    {
        var requested = sortableFields
            .FirstOrDefault(f => string.Equals(f.Key, request.SortBy, StringComparison.OrdinalIgnoreCase))
            .Value;
        var selector = requested ?? sortableFields[defaultSortBy];

        return request.IsDescending ? query.OrderByDescending(selector) : query.OrderBy(selector);
    }

    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, ListRequest request)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = request.PageSize < 1
            ? ListRequest.DefaultPageSize
            : Math.Min(request.PageSize, ListRequest.MaxPageSize);

        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    public static async Task<ListResult<T>> ToListResult<T>(
        this IQueryable<T> query,
        ListRequest request,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.ApplyPaging(request).ToListAsync(cancellationToken);

        return new ListResult<T>(items, totalCount);
    }
}
