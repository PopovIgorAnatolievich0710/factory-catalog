using System.Globalization;
using System.Linq.Expressions;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.Infrastructure.Common.Paging;

public static class ListFilterExpression
{
    public static Expression<Func<T, bool>>? Build<T, TValue>(
        string field,
        ListFilter filter,
        Expression<Func<T, TValue>> selector)
    {
        var body = BuildBody(field, filter, selector.Body);
        return body == null ? null : Expression.Lambda<Func<T, bool>>(body, selector.Parameters);
    }

    public static bool Matches<TValue>(string field, ListFilter filter, TValue value)
    {
        var predicate = Build<TValue, TValue>(field, filter, x => x);
        return predicate == null || predicate.Compile()(value);
    }

    private static Expression? BuildBody(string field, ListFilter filter, Expression member)
    {
        if (filter.Values is { Length: > 0 })
        {
            return filter.Values
                .Select(v => (Expression)Expression.Equal(member, Constant(field, v, member.Type)))
                .Aggregate(Expression.OrElse);
        }

        if (string.IsNullOrWhiteSpace(filter.Value))
        {
            return null;
        }

        var isText = member.Type == typeof(string);
        var op = filter.Operator?.ToLowerInvariant() ?? (isText ? ListFilterOperators.Contains : ListFilterOperators.Equal);

        if (op == ListFilterOperators.Contains)
        {
            return isText ? ContainsIgnoreCase(member, filter.Value) : throw Invalid(field, "Operator 'contains' applies to text fields only.");
        }

        if (isText && op != ListFilterOperators.Equal && op != ListFilterOperators.NotEqual)
        {
            throw Invalid(field, $"Operator '{op}' does not apply to text fields.");
        }

        var value = Constant(field, filter.Value, member.Type);

        return op switch
        {
            ListFilterOperators.Equal => Expression.Equal(member, value),
            ListFilterOperators.NotEqual => Expression.NotEqual(member, value),
            ListFilterOperators.LessThan => Expression.LessThan(member, value),
            ListFilterOperators.LessThanOrEqual => Expression.LessThanOrEqual(member, value),
            ListFilterOperators.GreaterThan => Expression.GreaterThan(member, value),
            ListFilterOperators.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, value),
            ListFilterOperators.Between => Expression.AndAlso(
                Expression.GreaterThanOrEqual(member, value),
                Expression.LessThanOrEqual(member, Constant(field, RequireValueTo(field, filter), member.Type))),
            _ => throw Invalid(field, $"Unknown operator '{filter.Operator}'.")
        };
    }

    private static Expression ContainsIgnoreCase(Expression member, string value)
    {
        var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

        return Expression.AndAlso(
            Expression.NotEqual(member, Expression.Constant(null, typeof(string))),
            Expression.Call(Expression.Call(member, toLower), contains, Expression.Constant(value.ToLowerInvariant())));
    }

    private static string RequireValueTo(string field, ListFilter filter)
    {
        return string.IsNullOrWhiteSpace(filter.ValueTo)
            ? throw Invalid(field, "Operator 'between' requires 'valueTo'.")
            : filter.ValueTo;
    }

    private static ConstantExpression Constant(string field, string raw, Type targetType)
    {
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            object parsed = type switch
            {
                _ when type == typeof(string) => raw,
                _ when type.IsEnum => Enum.Parse(type, raw, ignoreCase: true),
                _ when type == typeof(Guid) => Guid.Parse(raw),
                _ when type == typeof(DateOnly) => DateOnly.Parse(raw, CultureInfo.InvariantCulture),
                _ when type == typeof(DateTime) => DateTime.Parse(raw, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                _ => Convert.ChangeType(raw, type, CultureInfo.InvariantCulture)
            };

            return Expression.Constant(parsed, targetType);
        }
        catch (Exception e) when (e is FormatException or ArgumentException or InvalidCastException or OverflowException)
        {
            throw Invalid(field, $"Value '{raw}' is not valid for this field.");
        }
    }

    private static ValidationException Invalid(string field, string message)
    {
        return new ValidationException($"filters.{field}", message);
    }
}
