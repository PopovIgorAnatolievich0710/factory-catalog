using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Components;

internal class ComponentConfiguration : IEntityTypeConfiguration<Component>
{
    public void Configure(EntityTypeBuilder<Component> builder)
    {
        builder.ToTable("component");

        builder.HasKey(c => c.ErpId);

        builder.Property(c => c.ErpId).HasMaxLength(100);
        builder.Property(c => c.Name).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Family).HasMaxLength(100);
        builder.Property(c => c.Material).HasMaxLength(200);
        builder.Property(c => c.MeasuredBy).HasMaxLength(50);
        builder.Property(c => c.MeasureValue).HasPrecision(18, 4);
        builder.Property(c => c.MeasureUnit).HasMaxLength(20);
        builder.Property(c => c.UnitOfMeasureType).HasMaxLength(50);
        builder.Property(c => c.UnitOfMeasureQuantity).HasPrecision(18, 4);
        builder.Property(c => c.UnitOfMeasureUnit).HasMaxLength(20);
        builder.Property(c => c.CountryOfOrigin).HasMaxLength(2);
        builder.Property(c => c.HsCode).HasMaxLength(20);
        builder.Property(c => c.UnitCostAmount).HasPrecision(18, 4);
        builder.Property(c => c.UnitCostCurrency).HasMaxLength(3);
        builder.Property(c => c.UnitCostSource).HasMaxLength(50);
        builder.Property(c => c.CriticalStockLevel).HasPrecision(18, 4);

        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Family);
        builder.HasIndex(c => c.Status);
    }
}
