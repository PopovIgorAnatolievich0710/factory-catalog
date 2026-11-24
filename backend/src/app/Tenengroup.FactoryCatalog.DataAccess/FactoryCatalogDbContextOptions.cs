using Microsoft.EntityFrameworkCore;

namespace Tenengroup.FactoryCatalog.DataAccess;

public static class FactoryCatalogDbContextOptions
{
    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, string connectionString)
    {
        return builder
            .UseNpgsql(connectionString, options =>
            {
                options.MigrationsAssembly(typeof(FactoryCatalogDbContext).Assembly.GetName().Name);
                options.MigrationsHistoryTable("__EFMigrationsHistory", FactoryCatalogDbContext.SchemaName);
                options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                options.EnableRetryOnFailure();
            })
            .UseSnakeCaseNamingConvention();
    }
}
