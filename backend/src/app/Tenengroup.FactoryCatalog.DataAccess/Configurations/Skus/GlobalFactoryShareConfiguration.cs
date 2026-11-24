using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Skus;

internal class GlobalFactoryShareConfiguration : IEntityTypeConfiguration<GlobalFactoryShare>
{
    public void Configure(EntityTypeBuilder<GlobalFactoryShare> builder)
    {
        builder.ToTable("global_factory_share");

        builder.HasKey(s => s.FactoryId);

        builder.Property(s => s.FactoryId).HasMaxLength(20);
        builder.Property(s => s.Pct).HasPrecision(5, 2);
        builder.Property(s => s.UpdatedBy).HasMaxLength(320).IsRequired();

        builder.HasOne<Factory>()
            .WithMany()
            .HasForeignKey(s => s.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
