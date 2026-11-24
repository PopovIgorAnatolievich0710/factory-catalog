using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Shared;

internal class SyncRunConfiguration : IEntityTypeConfiguration<SyncRun>
{
    public void Configure(EntityTypeBuilder<SyncRun> builder)
    {
        builder.ToTable("sync_run");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SyncType).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.TriggeredBy).HasMaxLength(320).IsRequired();

        builder.HasIndex(s => new { s.SyncType, s.StartedAt });

        builder.HasIndex(s => s.SyncType)
            .IsUnique()
            .HasFilter("status = 'Running'")
            .HasDatabaseName("ux_sync_run_sync_type_running");
    }
}
