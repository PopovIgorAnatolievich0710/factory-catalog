using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components;

public record SetManualOosCommand(string ErpId, string FactoryId, bool Active, string? Note) : IRequest;
