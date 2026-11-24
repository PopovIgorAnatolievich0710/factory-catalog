using Microsoft.EntityFrameworkCore;

namespace Tenengroup.Balancer.DataAccess;

public static class BalancerDbContextOptions
{
    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, string connectionString)
    {
        return builder
            .UseNpgsql(connectionString, options =>
            {
                options.MigrationsAssembly(typeof(BalancerDbContext).Assembly.GetName().Name);
                options.MigrationsHistoryTable("__EFMigrationsHistory", BalancerDbContext.SchemaName);
                options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                options.EnableRetryOnFailure();
            })
            .UseSnakeCaseNamingConvention();
    }
}
