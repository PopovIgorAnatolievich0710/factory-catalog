using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Components;

internal class ComponentFactoryConfiguration : IEntityTypeConfiguration<ComponentFactory>
{
    public void Configure(EntityTypeBuilder<ComponentFactory> builder)
    {
        builder.ToTable("component_factory");

        builder.HasKey(f => new { f.ComponentErpId, f.FactoryId });

        builder.Property(f => f.ComponentErpId).HasMaxLength(100);
        builder.Property(f => f.FactoryId).HasMaxLength(20);
        builder.Property(f => f.WarehouseStock).HasPrecision(18, 4);
        builder.Property(f => f.ProductionFloorStock).HasPrecision(18, 4);
        builder.Property(f => f.CriticalStockLevelOverride).HasPrecision(18, 4);
        builder.Property(f => f.ManualOosSetBy).HasMaxLength(320);
        builder.Property(f => f.ManualOosNote).HasMaxLength(1000);

        builder.HasOne(f => f.Component)
            .WithMany(c => c.Factories)
            .HasForeignKey(f => f.ComponentErpId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Factory>()
            .WithMany()
            .HasForeignKey(f => f.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.FactoryId, f.ManualOosActive });
    }
}
