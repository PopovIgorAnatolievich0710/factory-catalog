using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;

namespace Tenengroup.Balancer.DataAccess;

public sealed class BalancerDbContext(DbContextOptions<BalancerDbContext> options)
    : DbContext(options), IBalancerDbContext
{
    public const string SchemaName = "balancer";

    public DbSet<ShippingRoute> ShippingRoutes { get; set; }
    public DbSet<Allocation> Allocations { get; set; }
    public DbSet<AllocationCostLine> AllocationCostLines { get; set; }
    public DbSet<LegacyDecision> LegacyDecisions { get; set; }
    public DbSet<CapacityReservation> CapacityReservations { get; set; }
    public DbSet<AllocationTransfer> AllocationTransfers { get; set; }
    public DbSet<ProductionRecord> ProductionRecords { get; set; }
    public DbSet<BalancerSettings> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
