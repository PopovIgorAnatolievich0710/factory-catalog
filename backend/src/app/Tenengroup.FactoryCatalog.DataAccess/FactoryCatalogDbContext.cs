using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.LegacyBalancer;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.DataAccess;

public sealed class FactoryCatalogDbContext(DbContextOptions<FactoryCatalogDbContext> options)
    : DbContext(options), IFactoryCatalogDbContext
{
    public const string SchemaName = "catalog";

    public DbSet<Factory> Factories { get; set; }
    public DbSet<UserFactory> UserFactories { get; set; }
    public DbSet<UserListSettings> UserListSettings { get; set; }
    public DbSet<HistoryEntry> HistoryEntries { get; set; }
    public DbSet<SyncRun> SyncRuns { get; set; }
    public DbSet<FeatureToggle> FeatureToggles { get; set; }

    public DbSet<Component> Components { get; set; }
    public DbSet<ComponentFactory> ComponentFactories { get; set; }
    public DbSet<ComponentAlternative> ComponentAlternatives { get; set; }
    public DbSet<ComponentReplacement> ComponentReplacements { get; set; }

    public DbSet<Process> Processes { get; set; }
    public DbSet<ProcessFactory> ProcessFactories { get; set; }

    public DbSet<Sku> Skus { get; set; }
    public DbSet<SkuAlias> SkuAliases { get; set; }
    public DbSet<SkuComponent> SkuComponents { get; set; }
    public DbSet<SkuProcess> SkuProcesses { get; set; }
    public DbSet<SkuFactory> SkuFactories { get; set; }
    public DbSet<GlobalFactoryShare> GlobalFactoryShares { get; set; }

    public DbSet<QuoteSet> QuoteSets { get; set; }
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<BalancerSkipLog> BalancerSkipLogs { get; set; }

    public void DiscardChanges() => ChangeTracker.Clear();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
