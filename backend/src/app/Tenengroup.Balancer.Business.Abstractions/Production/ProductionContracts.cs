using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.Balancer.Business.Abstractions.Production;

public record RecordProductionCommand(
    string SourceEventId,
    string OrderId,
    string OrderItemId,
    DateTime SentAt,
    bool IsReproduction,
    IReadOnlyCollection<SupplierProductionLogEntry> ProductionLog) : IRequest<bool>;

public record SupplierProductionLogEntry(string ProcessId, string? MachineType, decimal? Quantity);

public interface ISupplierQueueHandler
{
    Task Execute(CancellationToken cancellationToken);
}
