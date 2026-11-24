using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Shared;

internal class UserListSettingsConfiguration : IEntityTypeConfiguration<UserListSettings>
{
    public void Configure(EntityTypeBuilder<UserListSettings> builder)
    {
        builder.ToTable("user_list_settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserEmail).HasMaxLength(320).IsRequired();
        builder.Property(s => s.ListKey).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Columns).HasColumnType("text[]").IsRequired();

        builder.HasIndex(s => new { s.UserEmail, s.ListKey }).IsUnique();
    }
}
