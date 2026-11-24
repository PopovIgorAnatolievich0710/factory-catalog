using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Shared;

internal class UserFactoryConfiguration : IEntityTypeConfiguration<UserFactory>
{
    public void Configure(EntityTypeBuilder<UserFactory> builder)
    {
        builder.ToTable("user_factory");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserEmail).HasMaxLength(320).IsRequired();
        builder.Property(u => u.FactoryId).HasMaxLength(20).IsRequired();
        builder.Property(u => u.CreatedBy).HasMaxLength(320).IsRequired();
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(u => u.Factory)
            .WithMany()
            .HasForeignKey(u => u.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => new { u.UserEmail, u.FactoryId }).IsUnique();
    }
}
