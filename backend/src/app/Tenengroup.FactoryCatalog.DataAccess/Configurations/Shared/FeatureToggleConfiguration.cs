using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Shared;

internal class FeatureToggleConfiguration : IEntityTypeConfiguration<FeatureToggle>
{
    public void Configure(EntityTypeBuilder<FeatureToggle> builder)
    {
        builder.ToTable("feature_toggle");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(t => t.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(t => t.Name).IsUnique();
    }
}
