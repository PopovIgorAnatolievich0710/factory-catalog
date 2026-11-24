using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Components;

internal class ComponentAlternativeConfiguration : IEntityTypeConfiguration<ComponentAlternative>
{
    public void Configure(EntityTypeBuilder<ComponentAlternative> builder)
    {
        builder.ToTable("component_alternative");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ComponentErpId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.AlternativeErpId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.CreatedBy).HasMaxLength(320).IsRequired();

        builder.HasOne(a => a.Component)
            .WithMany(c => c.Alternatives)
            .HasForeignKey(a => a.ComponentErpId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Alternative)
            .WithMany()
            .HasForeignKey(a => a.AlternativeErpId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ComponentErpId, a.AlternativeErpId }).IsUnique();
        builder.HasIndex(a => a.AlternativeErpId);
    }
}
