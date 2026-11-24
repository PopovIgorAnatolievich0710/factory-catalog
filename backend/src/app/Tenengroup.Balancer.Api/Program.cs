using Microsoft.EntityFrameworkCore;
using Tenengroup.Balancer.DataAccess;
using Tenengroup.Infrastructure.Common;
using Tenengroup.Infrastructure.Common.Web;

namespace Tenengroup.Balancer.Api;

public class Program
{
    private const string MigrateCommand = "migrate";

    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        if (args.FirstOrDefault() == MigrateCommand)
        {
            await Migrate(host.Services.GetRequiredService<IConfiguration>());
            return;
        }

        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging => logging.ClearProviders())
            .UseConfiguredSerilog()
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }

    private static async Task Migrate(IConfiguration configuration)
    {
        var builder = new DbContextOptionsBuilder<BalancerDbContext>();
        BalancerDbContextOptions.Configure(builder, DbConnectionString.Build(configuration));

        await using var db = new BalancerDbContext(builder.Options);
        await db.Database.MigrateAsync();
    }
}
