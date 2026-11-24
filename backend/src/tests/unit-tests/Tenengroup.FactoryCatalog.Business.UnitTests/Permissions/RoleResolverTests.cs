using Moq;
using NUnit.Framework;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Permissions;

[TestFixture]
public class RoleResolverTests
{
    private static RoleResolver CreateResolver(GroupPermissions permissions)
    {
        var provider = new Mock<IGroupPermissionsProvider>();
        provider.Setup(p => p.Get(It.IsAny<CancellationToken>())).ReturnsAsync(permissions);

        return new RoleResolver(provider.Object);
    }

    [Test]
    public async Task Resolve_NoDocument_NotReady()
    {
        var result = await CreateResolver(null).Resolve(["any"]);

        Assert.That(result.IsAuthorizationReady, Is.False);
    }

    [Test]
    public async Task Resolve_MapsAliasesAndDropsUnknownGroups()
    {
        var resolver = CreateResolver(new GroupPermissions
        {
            RoleGroupMapping = new Dictionary<string, string[]>
            {
                [RoleNames.FactoryManager] = [RoleNames.FactoryManager, "entra-guid-1"],
                [RoleNames.Finance] = [RoleNames.Finance]
            }
        });

        var result = await resolver.Resolve(["ENTRA-GUID-1", "unknown"]);

        Assert.That(result.IsAuthorizationReady, Is.True);
        Assert.That(result.Roles, Is.EqualTo(new[] { RoleNames.FactoryManager }));
    }

    [Test]
    public async Task Resolve_RestrictedGroup_IsRestrictedWithoutRoles()
    {
        var resolver = CreateResolver(new GroupPermissions
        {
            RoleGroupMapping = new Dictionary<string, string[]> { [RoleNames.Finance] = [RoleNames.Finance] },
            RestrictedGroups = ["blocked"]
        });

        var result = await resolver.Resolve([RoleNames.Finance, "Blocked"]);

        Assert.That(result.IsRestricted, Is.True);
        Assert.That(result.Roles, Is.Empty);
    }
}
