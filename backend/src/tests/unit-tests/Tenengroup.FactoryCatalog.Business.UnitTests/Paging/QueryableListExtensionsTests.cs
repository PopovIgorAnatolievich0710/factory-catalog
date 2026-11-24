using System.Linq.Expressions;
using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Paging;

[TestFixture]
public class QueryableListExtensionsTests
{
    private static readonly Dictionary<string, Expression<Func<Factory, object>>> SortableFields = new()
    {
        ["id"] = f => f.Id,
        ["name"] = f => f.Name
    };

    private static readonly Factory[] Factories =
    [
        new() { Id = "125", Name = "B" },
        new() { Id = "110", Name = "C" },
        new() { Id = "315", Name = "A" }
    ];

    [Test]
    public void ApplySorting_RequestedFieldDescending()
    {
        var request = new ListRequest { SortBy = "Name", SortDirection = "desc" };

        var result = Factories.AsQueryable().ApplySorting(request, SortableFields, "id").Select(f => f.Name);

        Assert.That(result, Is.EqualTo(new[] { "C", "B", "A" }));
    }

    [Test]
    public void ApplySorting_UnknownField_UsesDefaultAscending()
    {
        var request = new ListRequest { SortBy = "unknown" };

        var result = Factories.AsQueryable().ApplySorting(request, SortableFields, "id").Select(f => f.Id);

        Assert.That(result, Is.EqualTo(new[] { "110", "125", "315" }));
    }

    [TestCase(0, 0, 3)]
    [TestCase(2, 2, 1)]
    [TestCase(1, 100000, 3)]
    public void ApplyPaging_ClampsPageAndSize(int page, int pageSize, int expectedCount)
    {
        var request = new ListRequest { Page = page, PageSize = pageSize };

        Assert.That(Factories.AsQueryable().ApplyPaging(request).Count(), Is.EqualTo(expectedCount));
    }

    [Test]
    public async Task ToListResult_ReturnsPageAndTotal()
    {
        var db = new Mock<IFactoryCatalogDbContext>();
        db.SetupSet(x => x.Factories, Factories);
        var request = new ListRequest { Page = 2, PageSize = 2 };

        var result = await db.Object.Factories.OrderBy(f => f.Id).ToListResult(request, CancellationToken.None);

        Assert.That(result.TotalCount, Is.EqualTo(3));
        Assert.That(result.Items.Select(f => f.Id), Is.EqualTo(new[] { "315" }));
    }
}
