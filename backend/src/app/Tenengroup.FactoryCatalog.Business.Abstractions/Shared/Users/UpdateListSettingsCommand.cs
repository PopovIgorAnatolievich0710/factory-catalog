using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;

public record UpdateListSettingsCommand(string ListKey, IReadOnlyCollection<string> Columns) : IRequest<ListSettingsDto>;
