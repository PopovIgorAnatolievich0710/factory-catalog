using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests;

internal static class TestDb
{
    public static List<T> SetupSet<T>(
        this Mock<IFactoryCatalogDbContext> db,
        Expression<Func<IFactoryCatalogDbContext, DbSet<T>>> set,
        params T[] items) where T : class
    {
        var list = items.ToList();
        var dbSet = new Mock<DbSet<T>>();
        db.Setup(set).ReturnsDbSet(list, dbSet);
        dbSet.Setup(s => s.Add(It.IsAny<T>())).Callback<T>(list.Add);

        return list;
    }

    public static TimeProvider FixedTime(DateTime utcNow)
    {
        var time = new Mock<TimeProvider> { CallBase = true };
        time.Setup(t => t.GetUtcNow()).Returns(new DateTimeOffset(utcNow, TimeSpan.Zero));

        return time.Object;
    }
}
