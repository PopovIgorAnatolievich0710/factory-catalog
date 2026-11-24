using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;

public record GetListSettingsQuery(string ListKey) : IRequest<ListSettingsDto>;
