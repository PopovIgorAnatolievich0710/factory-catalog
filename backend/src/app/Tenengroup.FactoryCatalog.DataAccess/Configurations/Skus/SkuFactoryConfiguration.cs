using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Skus;

internal class SkuFactoryConfiguration : IEntityTypeConfiguration<SkuFactory>
{
    public void Configure(EntityTypeBuilder<SkuFactory> builder)
    {
        builder.ToTable("sku_factory");

        builder.HasKey(f => new { f.SkuUuid, f.FactoryId });

        builder.Property(f => f.FactoryId).HasMaxLength(20);
        builder.Property(f => f.OverrideSharePct).HasPrecision(5, 2);
        builder.Property(f => f.OverrideBy).HasMaxLength(320);

        builder.HasOne(f => f.Sku)
            .WithMany(s => s.Factories)
            .HasForeignKey(f => f.SkuUuid)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Factory>()
            .WithMany()
            .HasForeignKey(f => f.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.FactoryId, f.Producible });
    }
}
