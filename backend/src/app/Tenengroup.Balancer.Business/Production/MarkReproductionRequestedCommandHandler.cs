using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions.Production;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.Balancer.Business.Production;

public class MarkReproductionRequestedCommandHandler(
    IBalancerDbContext db,
    ILogger<MarkReproductionRequestedCommandHandler> logger) : IRequestHandler<MarkReproductionRequestedCommand, bool>
{
    public async Task<bool> Handle(MarkReproductionRequestedCommand request, CancellationToken cancellationToken)
    {
        var allocation = await db.Allocations
            .Where(a => a.OrderId == request.OrderId
                        && a.OrderItemId == request.OrderItemId
                        && a.Mode != AllocationMode.Shadow
                        && a.FactoryId != null)
            .OrderByDescending(a => a.DecidedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (allocation == null)
        {
            logger.LogWarning("Reproduction of order {OrderId} item {OrderItemId}: no live allocation",
                request.OrderId, request.OrderItemId);
            return true;
        }

        if (allocation.ReproductionRequestedAt >= request.RequestedAt)
        {
            return true;
        }

        allocation.ReproductionRequestedAt = request.RequestedAt;
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
