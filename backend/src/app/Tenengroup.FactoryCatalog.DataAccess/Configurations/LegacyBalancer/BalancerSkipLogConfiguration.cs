using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.LegacyBalancer;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.LegacyBalancer;

internal class BalancerSkipLogConfiguration : IEntityTypeConfiguration<BalancerSkipLog>
{
    public void Configure(EntityTypeBuilder<BalancerSkipLog> builder)
    {
        builder.ToTable("balancer_skip_log");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.OrderId).HasMaxLength(100).IsRequired();
        builder.Property(l => l.OrderItemId).HasMaxLength(100);
        builder.Property(l => l.FactoryId).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Reason).HasConversion<string>().HasMaxLength(30);
        builder.Property(l => l.ComponentErpId).HasMaxLength(100);
        builder.Property(l => l.ProcessId).HasMaxLength(50);

        builder.HasIndex(l => l.OrderId);
        builder.HasIndex(l => l.CreatedAt);
    }
}
