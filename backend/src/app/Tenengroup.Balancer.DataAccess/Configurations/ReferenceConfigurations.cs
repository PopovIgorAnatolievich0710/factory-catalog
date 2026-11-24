using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.Balancer.Domains;

namespace Tenengroup.Balancer.DataAccess.Configurations;

internal class ShippingRouteConfiguration : IEntityTypeConfiguration<ShippingRoute>
{
    public void Configure(EntityTypeBuilder<ShippingRoute> builder)
    {
        builder.ToTable("shipping_route_cache");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.OriginFacilityId).HasMaxLength(20).IsRequired();
        builder.Property(r => r.DestinationCountry).HasMaxLength(2).IsRequired();
        builder.Property(r => r.CostAmount).HasPrecision(18, 4);
        builder.Property(r => r.CostCurrency).HasMaxLength(3).IsRequired();
        builder.HasIndex(r => new { r.OriginFacilityId, r.DestinationCountry }).IsUnique();
        builder.HasIndex(r => r.DestinationCountry);
    }
}

internal class LegacyDecisionConfiguration : IEntityTypeConfiguration<LegacyDecision>
{
    public void Configure(EntityTypeBuilder<LegacyDecision> builder)
    {
        builder.ToTable("legacy_decision");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.OrderId).HasMaxLength(100).IsRequired();
        builder.Property(d => d.OrderItemId).HasMaxLength(100).IsRequired();
        builder.Property(d => d.FactoryId).HasMaxLength(20).IsRequired();
        builder.Property(d => d.TotalCost).HasPrecision(18, 4);
        builder.HasIndex(d => new { d.OrderId, d.OrderItemId }).IsUnique();
    }
}

internal class CapacityReservationConfiguration : IEntityTypeConfiguration<CapacityReservation>
{
    public void Configure(EntityTypeBuilder<CapacityReservation> builder)
    {
        builder.ToTable("capacity_reservation");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.FactoryId).HasMaxLength(20).IsRequired();
        builder.Property(r => r.EventName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.UpdatedBy).HasMaxLength(320).IsRequired();
        builder.HasIndex(r => new { r.FactoryId, r.DateFrom, r.DateTo });
    }
}

internal class BalancerSettingsConfiguration : IEntityTypeConfiguration<BalancerSettings>
{
    public void Configure(EntityTypeBuilder<BalancerSettings> builder)
    {
        builder.ToTable("balancer_settings");
        builder.HasKey(s => s.Version);
        builder.Property(s => s.Version).ValueGeneratedNever();
        builder.Property(s => s.Mode).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.LatePenaltyPerDay).HasPrecision(18, 4);
        builder.Property(s => s.SplitPenalty).HasPrecision(18, 4);
        builder.Property(s => s.ShareWeight).HasPrecision(9, 4);
        builder.Property(s => s.SuccessRateWeight).HasPrecision(9, 4);
        builder.Property(s => s.ShareGapThresholdPct).HasPrecision(5, 2);
        builder.Property(s => s.UpdatedBy).HasMaxLength(320).IsRequired();
    }
}
