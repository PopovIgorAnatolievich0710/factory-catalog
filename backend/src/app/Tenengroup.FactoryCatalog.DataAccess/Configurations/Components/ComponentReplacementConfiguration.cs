using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Components;

internal class ComponentReplacementConfiguration : IEntityTypeConfiguration<ComponentReplacement>
{
    public void Configure(EntityTypeBuilder<ComponentReplacement> builder)
    {
        builder.ToTable("component_replacement");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrderId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.OrderItemId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.FactoryId).HasMaxLength(20).IsRequired();
        builder.Property(r => r.OriginalErpId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.ReplacementErpId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Quantity).HasPrecision(18, 4);
        builder.Property(r => r.ReplacedBy).HasMaxLength(320).IsRequired();

        builder.HasIndex(r => new { r.OrderId, r.OrderItemId });
        builder.HasIndex(r => r.OriginalErpId);
    }
}
