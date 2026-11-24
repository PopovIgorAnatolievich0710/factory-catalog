using NUnit.Framework;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Paging;

[TestFixture]
public class ListFilterTests
{
    private enum Status
    {
        Active,
        Inactive
    }

    private record Row(string Name, decimal? Stock, Status Status, int Capacity);

    private static readonly Row[] Rows =
    [
        new("White gold board", 12.5m, Status.Active, 100),
        new("Silver chain", 2m, Status.Inactive, 40),
        new("Gold clasp", null, Status.Active, 0),
        new(null, 7m, Status.Active, 70)
    ];

    private static string[] Apply<TValue>(
        ListFilter filter,
        System.Linq.Expressions.Expression<Func<Row, TValue>> selector)
    {
        var request = new ListRequest { Filters = { ["Field"] = filter } };

        return Rows.AsQueryable().ApplyFilter(request, "field", selector).Select(r => r.Name).ToArray();
    }

    [Test]
    public void NoFilterForField_ReturnsEverything()
    {
        var result = Rows.AsQueryable().ApplyFilter(new ListRequest(), "name", r => r.Name).ToArray();

        Assert.That(result, Has.Length.EqualTo(Rows.Length));
    }

    [Test]
    public void Values_OnEnum_MatchesAnyIgnoringCase()
    {
        var result = Apply(new ListFilter { Values = ["inactive"] }, r => r.Status);

        Assert.That(result, Is.EqualTo(new[] { "Silver chain" }));
    }

    [Test]
    public void Values_OnText_MatchesAny()
    {
        var result = Apply(new ListFilter { Values = ["Silver chain", "Gold clasp"] }, r => r.Name);

        Assert.That(result, Is.EqualTo(new[] { "Silver chain", "Gold clasp" }));
    }

    [Test]
    public void Contains_OnText_IsCaseInsensitiveAndSkipsNulls()
    {
        var result = Apply(new ListFilter { Operator = "contains", Value = "GOLD" }, r => r.Name);

        Assert.That(result, Is.EqualTo(new[] { "White gold board", "Gold clasp" }));
    }

    [Test]
    public void TextWithoutOperator_DefaultsToContains()
    {
        var result = Apply(new ListFilter { Value = "chain" }, r => r.Name);

        Assert.That(result, Is.EqualTo(new[] { "Silver chain" }));
    }

    [TestCase("eq", "40", new[] { "Silver chain" })]
    [TestCase("ne", "40", new[] { "White gold board", "Gold clasp", null })]
    [TestCase("lt", "40", new[] { "Gold clasp" })]
    [TestCase("lte", "40", new[] { "Silver chain", "Gold clasp" })]
    [TestCase("gt", "70", new[] { "White gold board" })]
    [TestCase("gte", "70", new[] { "White gold board", null })]
    public void NumberOperators(string op, string value, string[] expected)
    {
        var result = Apply(new ListFilter { Operator = op, Value = value }, r => r.Capacity);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Between_OnNullableDecimal_IsInclusiveAndSkipsNulls()
    {
        var result = Apply(new ListFilter { Operator = "between", Value = "2", ValueTo = "7.5" }, r => r.Stock);

        Assert.That(result, Is.EqualTo(new[] { "Silver chain", null }));
    }

    [Test]
    public void Between_WithoutValueTo_IsInvalid()
    {
        Assert.Throws<ValidationException>(() => Apply(new ListFilter { Operator = "between", Value = "2" }, r => r.Stock));
    }

    [Test]
    public void NotANumber_IsInvalid()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            Apply(new ListFilter { Operator = "gt", Value = "abc" }, r => r.Capacity));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "filters.field" }));
    }

    [Test]
    public void UnknownOperator_IsInvalid()
    {
        Assert.Throws<ValidationException>(() => Apply(new ListFilter { Operator = "like", Value = "1" }, r => r.Capacity));
    }

    [Test]
    public void ContainsOnNumber_IsInvalid()
    {
        Assert.Throws<ValidationException>(() => Apply(new ListFilter { Operator = "contains", Value = "1" }, r => r.Capacity));
    }

    [Test]
    public void EmptyFilter_ReturnsEverything()
    {
        Assert.That(Apply(new ListFilter(), r => r.Capacity), Has.Length.EqualTo(Rows.Length));
    }

    [Test]
    public void Matches_ForDerivedValues()
    {
        var filter = new ListFilter { Operator = "lt", Value = "10" };

        Assert.That(ListFilterExpression.Matches("totalStock", filter, 9.5m), Is.True);
        Assert.That(ListFilterExpression.Matches("totalStock", filter, 10m), Is.False);
    }
}
