using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Skus;

internal class SkuAliasConfiguration : IEntityTypeConfiguration<SkuAlias>
{
    public void Configure(EntityTypeBuilder<SkuAlias> builder)
    {
        builder.ToTable("sku_alias");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Alias).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(a => a.Sku)
            .WithMany(s => s.Aliases)
            .HasForeignKey(a => a.SkuUuid)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.Alias).IsUnique();
        builder.HasIndex(a => a.SkuUuid);
    }
}
