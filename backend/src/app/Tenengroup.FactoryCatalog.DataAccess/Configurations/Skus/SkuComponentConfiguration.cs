using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Skus;

internal class SkuComponentConfiguration : IEntityTypeConfiguration<SkuComponent>
{
    public void Configure(EntityTypeBuilder<SkuComponent> builder)
    {
        builder.ToTable("sku_component");

        builder.HasKey(c => new { c.SkuUuid, c.ComponentErpId });

        builder.Property(c => c.ComponentErpId).HasMaxLength(100);
        builder.Property(c => c.Quantity).HasPrecision(18, 4);
        builder.Property(c => c.Usage).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(c => c.Sku)
            .WithMany(s => s.Components)
            .HasForeignKey(c => c.SkuUuid)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.ComponentErpId);
    }
}
