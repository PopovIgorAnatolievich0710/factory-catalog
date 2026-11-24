using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.LegacyBalancer;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.DataAccess.Abstractions;

public interface IFactoryCatalogDbContext
{
    DbSet<Factory> Factories { get; set; }
    DbSet<UserFactory> UserFactories { get; set; }
    DbSet<UserListSettings> UserListSettings { get; set; }
    DbSet<HistoryEntry> HistoryEntries { get; set; }
    DbSet<SyncRun> SyncRuns { get; set; }
    DbSet<FeatureToggle> FeatureToggles { get; set; }

    DbSet<Component> Components { get; set; }
    DbSet<ComponentFactory> ComponentFactories { get; set; }
    DbSet<ComponentAlternative> ComponentAlternatives { get; set; }
    DbSet<ComponentReplacement> ComponentReplacements { get; set; }

    DbSet<Process> Processes { get; set; }
    DbSet<ProcessFactory> ProcessFactories { get; set; }

    DbSet<Sku> Skus { get; set; }
    DbSet<SkuAlias> SkuAliases { get; set; }
    DbSet<SkuComponent> SkuComponents { get; set; }
    DbSet<SkuProcess> SkuProcesses { get; set; }
    DbSet<SkuFactory> SkuFactories { get; set; }
    DbSet<GlobalFactoryShare> GlobalFactoryShares { get; set; }

    DbSet<QuoteSet> QuoteSets { get; set; }
    DbSet<Quote> Quotes { get; set; }
    DbSet<BalancerSkipLog> BalancerSkipLogs { get; set; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    void DiscardChanges();
}
