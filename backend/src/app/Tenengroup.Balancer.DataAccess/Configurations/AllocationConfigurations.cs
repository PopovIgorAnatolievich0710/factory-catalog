using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.Balancer.Domains;

namespace Tenengroup.Balancer.DataAccess.Configurations;

internal class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.ToTable("allocation");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.OrderId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.OrderItemId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.SkuUuid).HasMaxLength(100).IsRequired();
        builder.Property(a => a.FulfillmentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.DestinationCountry).HasMaxLength(2).IsRequired();
        builder.Property(a => a.EventName).HasMaxLength(200);
        builder.Property(a => a.Site).HasMaxLength(100);
        builder.Property(a => a.CbdJson).HasColumnName("cbd").HasColumnType("jsonb").IsRequired();
        builder.Property(a => a.Mode).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.FallbackReason).HasMaxLength(500);
        builder.Property(a => a.QuoteKey).HasMaxLength(200);
        builder.Property(a => a.SelectedQuoteId).HasMaxLength(220);
        builder.Property(a => a.FactoryId).HasMaxLength(20);
        builder.Property(a => a.OriginalFactoryId).HasMaxLength(20);
        builder.Property(a => a.PackingFacilityId).HasMaxLength(20);
        builder.Property(a => a.ProductionPage).HasMaxLength(100);
        builder.Property(a => a.MaterialCost).HasPrecision(18, 4);
        builder.Property(a => a.LaborCost).HasPrecision(18, 4);
        builder.Property(a => a.PackingCost).HasPrecision(18, 4);
        builder.Property(a => a.ProductionCost).HasPrecision(18, 4);
        builder.Property(a => a.ShippingCost).HasPrecision(18, 4);
        builder.Property(a => a.TotalCost).HasPrecision(18, 4);
        builder.Property(a => a.CheapestFeasibleTotalCost).HasPrecision(18, 4);
        builder.Property(a => a.ActualLaborCost).HasPrecision(18, 4);
        builder.Property(a => a.Currency).HasMaxLength(3);
        builder.Property(a => a.DecisionReason).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.CandidatesJson).HasColumnName("candidates").HasColumnType("jsonb").IsRequired();
        builder.HasIndex(a => new { a.OrderId, a.OrderItemId });
        builder.HasIndex(a => a.DecidedAt);
        builder.HasIndex(a => new { a.FactoryId, a.Status, a.DueDate });
    }
}

internal class AllocationCostLineConfiguration : IEntityTypeConfiguration<AllocationCostLine>
{
    public void Configure(EntityTypeBuilder<AllocationCostLine> builder)
    {
        builder.ToTable("allocation_cost_line");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Code).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.QuotedUnitCost).HasPrecision(18, 4);
        builder.Property(l => l.QuotedCost).HasPrecision(18, 4);
        builder.Property(l => l.ActualQuantity).HasPrecision(18, 4);
        builder.Property(l => l.ActualCost).HasPrecision(18, 4);
        builder.HasOne(l => l.Allocation)
            .WithMany(a => a.CostLines)
            .HasForeignKey(l => l.AllocationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal class AllocationTransferConfiguration : IEntityTypeConfiguration<AllocationTransfer>
{
    public void Configure(EntityTypeBuilder<AllocationTransfer> builder)
    {
        builder.ToTable("allocation_transfer");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.OrderId).HasMaxLength(100).IsRequired();
        builder.Property(t => t.OrderItemId).HasMaxLength(100).IsRequired();
        builder.Property(t => t.SkuUuid).HasMaxLength(100).IsRequired();
        builder.Property(t => t.FromFactoryId).HasMaxLength(20).IsRequired();
        builder.Property(t => t.FromPage).HasMaxLength(100);
        builder.Property(t => t.ToFactoryId).HasMaxLength(20).IsRequired();
        builder.Property(t => t.ToPage).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Reason).HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.Note).HasMaxLength(1000);
        builder.Property(t => t.OldQuoteId).HasMaxLength(220);
        builder.Property(t => t.NewQuoteId).HasMaxLength(220);
        builder.Property(t => t.CostDelta).HasPrecision(18, 4);
        builder.Property(t => t.Currency).HasMaxLength(3);
        builder.Property(t => t.TransferredBy).HasMaxLength(320).IsRequired();
        builder.Property(t => t.Initiator).HasConversion<string>().HasMaxLength(20);
        builder.HasOne<Allocation>()
            .WithMany()
            .HasForeignKey(t => t.AllocationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(t => t.TransferredAt);
        builder.HasIndex(t => new { t.OrderId, t.OrderItemId });
    }
}

internal class ProductionRecordConfiguration : IEntityTypeConfiguration<ProductionRecord>
{
    public void Configure(EntityTypeBuilder<ProductionRecord> builder)
    {
        builder.ToTable("production_record");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.SourceEventId).HasMaxLength(200).IsRequired();
        builder.Property(r => r.OrderId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.OrderItemId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.SkuUuid).HasMaxLength(100).IsRequired();
        builder.Property(r => r.QuoteKey).HasMaxLength(200).IsRequired();
        builder.Property(r => r.QuoteId).HasMaxLength(220).IsRequired();
        builder.Property(r => r.FactoryId).HasMaxLength(20).IsRequired();
        builder.Property(r => r.PackingFacilityId).HasMaxLength(20).IsRequired();
        builder.Property(r => r.ReproductionOfQuoteId).HasMaxLength(220);
        builder.Property(r => r.ProductionLogJson).HasColumnName("production_log").HasColumnType("jsonb").IsRequired();
        builder.HasOne<Allocation>()
            .WithMany()
            .HasForeignKey(r => r.AllocationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProductionRecord>()
            .WithMany()
            .HasForeignKey(r => r.ReproductionOfRecordId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.SourceEventId).IsUnique();
        builder.HasIndex(r => new { r.FactoryId, r.SentAt });
    }
}
