using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Shared;

internal class HistoryEntryConfiguration : IEntityTypeConfiguration<HistoryEntry>
{
    public void Configure(EntityTypeBuilder<HistoryEntry> builder)
    {
        builder.ToTable("history_entry");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(h => h.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(h => h.FactoryId).HasMaxLength(20);
        builder.Property(h => h.Field).HasMaxLength(100).IsRequired();
        builder.Property(h => h.ChangedBy).HasMaxLength(320).IsRequired();
        builder.Property(h => h.Source).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(h => new { h.EntityType, h.EntityId, h.ChangedAt });
    }
}
