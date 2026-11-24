using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;

public record GetCurrentUserQuery : IRequest<CurrentUserDto>;
