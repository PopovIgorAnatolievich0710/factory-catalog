using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tenengroup.FactoryCatalog.DataAccess;

public class FactoryCatalogDbContextFactory : IDesignTimeDbContextFactory<FactoryCatalogDbContext>
{
    private const string DesignTimeConnectionString = "Server=localhost;Port=5432;Database=factory_catalog;User Id=postgres;Password=postgres;";

    public FactoryCatalogDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<FactoryCatalogDbContext>();
        FactoryCatalogDbContextOptions.Configure(builder, DesignTimeConnectionString);

        return new FactoryCatalogDbContext(builder.Options);
    }
}
