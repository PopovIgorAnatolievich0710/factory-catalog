using Microsoft.EntityFrameworkCore;
using Tenengroup.Balancer.Domains;

namespace Tenengroup.Balancer.DataAccess.Abstractions;

public interface IBalancerDbContext
{
    DbSet<ShippingRoute> ShippingRoutes { get; set; }
    DbSet<Allocation> Allocations { get; set; }
    DbSet<AllocationCostLine> AllocationCostLines { get; set; }
    DbSet<LegacyDecision> LegacyDecisions { get; set; }
    DbSet<CapacityReservation> CapacityReservations { get; set; }
    DbSet<AllocationTransfer> AllocationTransfers { get; set; }
    DbSet<ProductionRecord> ProductionRecords { get; set; }
    DbSet<BalancerSettings> Settings { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
