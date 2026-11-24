using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Skus;

internal class SkuConfiguration : IEntityTypeConfiguration<Sku>
{
    public void Configure(EntityTypeBuilder<Sku> builder)
    {
        builder.ToTable("sku");

        builder.HasKey(s => s.SkuUuid);

        builder.Property(s => s.SkuUuid).ValueGeneratedNever();
        builder.Property(s => s.SiteSku).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(500).IsRequired();
        builder.Property(s => s.ProductType).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.DropshipperId).HasMaxLength(100);
        builder.Property(s => s.DropshipperName).HasMaxLength(200);
        builder.Property(s => s.DropshipperTotalCostAmount).HasPrecision(18, 4);
        builder.Property(s => s.DropshipperTotalCostCurrency).HasMaxLength(3);
        builder.Property(s => s.ValidationStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.ValidationIssues).HasColumnType("text[]").IsRequired();

        builder.HasIndex(s => s.SiteSku);
        builder.HasIndex(s => s.Name);
        builder.HasIndex(s => new { s.ProductType, s.Status });
        builder.HasIndex(s => s.ValidationStatus);
    }
}
