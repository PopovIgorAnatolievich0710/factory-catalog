using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Skus;

internal class SkuProcessConfiguration : IEntityTypeConfiguration<SkuProcess>
{
    public void Configure(EntityTypeBuilder<SkuProcess> builder)
    {
        builder.ToTable("sku_process");

        builder.HasKey(p => new { p.SkuUuid, p.ProcessId });

        builder.Property(p => p.ProcessId).HasMaxLength(50);
        builder.Property(p => p.Quantity).HasPrecision(18, 4);

        builder.HasOne(p => p.Sku)
            .WithMany(s => s.Processes)
            .HasForeignKey(p => p.SkuUuid)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.ProcessId);
    }
}
