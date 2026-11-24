using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tenengroup.Balancer.DataAccess;

public class BalancerDbContextFactory : IDesignTimeDbContextFactory<BalancerDbContext>
{
    private const string DesignTimeConnectionString = "Server=localhost;Port=5432;Database=factory_catalog;User Id=postgres;Password=postgres;";

    public BalancerDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<BalancerDbContext>();
        BalancerDbContextOptions.Configure(builder, DesignTimeConnectionString);

        return new BalancerDbContext(builder.Options);
    }
}
