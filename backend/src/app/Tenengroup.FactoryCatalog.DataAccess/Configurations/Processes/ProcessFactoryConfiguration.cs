using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Processes;

internal class ProcessFactoryConfiguration : IEntityTypeConfiguration<ProcessFactory>
{
    public void Configure(EntityTypeBuilder<ProcessFactory> builder)
    {
        builder.ToTable("process_factory", t => t.HasCheckConstraint(
            "ck_process_factory_capacity_pct",
            "capacity_pct BETWEEN 0 AND 100 AND capacity_pct % 10 = 0"));

        builder.HasKey(f => new { f.ProcessId, f.FactoryId });

        builder.Property(f => f.ProcessId).HasMaxLength(50);
        builder.Property(f => f.FactoryId).HasMaxLength(20);
        builder.Property(f => f.MachineType).HasMaxLength(100);
        builder.Property(f => f.CapacityPct).HasDefaultValue(ProcessFactory.FullCapacityPct);
        builder.Property(f => f.CapacityReason).HasMaxLength(1000);
        builder.Property(f => f.CapacityUpdatedBy).HasMaxLength(320);
        builder.Property(f => f.MaxPerDay).HasPrecision(18, 4);
        builder.Property(f => f.LaborCostAmount).HasPrecision(18, 4);
        builder.Property(f => f.LaborCostCurrency).HasMaxLength(3);

        builder.HasOne(f => f.Process)
            .WithMany(p => p.Factories)
            .HasForeignKey(f => f.ProcessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Factory>()
            .WithMany()
            .HasForeignKey(f => f.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.FactoryId);
    }
}
