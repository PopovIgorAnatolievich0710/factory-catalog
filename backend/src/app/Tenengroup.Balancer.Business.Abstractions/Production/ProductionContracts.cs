using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.Balancer.Business.Abstractions.Production;

public record RecordProductionCommand(
    string SourceEventId,
    string OrderId,
    string OrderItemId,
    DateTime SentAt,
    bool IsReproduction,
    IReadOnlyCollection<ProductionLogEntry> ProductionLog) : IRequest<bool>;

public interface ISupplierQueueHandler
{
    Task Execute(CancellationToken cancellationToken);
}
