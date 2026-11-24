using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Processes;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Processes;

internal class ProcessConfiguration : IEntityTypeConfiguration<Process>
{
    private static readonly DateTime SeededAt = new(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Process> builder)
    {
        builder.ToTable("process");

        builder.HasKey(p => p.ProcessId);

        builder.Property(p => p.ProcessId).HasMaxLength(50);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.MeasuredIn).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => p.Name).IsUnique();

        builder.HasData(new Process
        {
            ProcessId = Process.PackingProcessId,
            Name = "Packing",
            Type = ProcessType.Work,
            MeasuredIn = ProcessUnit.Units,
            Description = "Packing of a finished order item",
            Status = ProcessStatus.Active,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        });
    }
}
