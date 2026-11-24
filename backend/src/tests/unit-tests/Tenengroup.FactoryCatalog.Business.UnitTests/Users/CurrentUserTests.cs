using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Users;

[TestFixture]
public class CurrentUserTests
{
    private const string Email = "fm.110@tenengroup.com";

    private Mock<IUserIdentity> identity;
    private Mock<IRoleResolver> roleResolver;
    private Mock<IPermissionEnforcementToggle> toggle;
    private Mock<IFactoryCatalogDbContext> db;

    [SetUp]
    public void SetUp()
    {
        identity = new Mock<IUserIdentity>();
        identity.Setup(i => i.Email).Returns(Email);
        identity.Setup(i => i.Name).Returns("Factory Manager");
        identity.Setup(i => i.Groups).Returns(["group"]);

        roleResolver = new Mock<IRoleResolver>();
        toggle = new Mock<IPermissionEnforcementToggle>();
        toggle.Setup(t => t.IsEnforced(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        db = new Mock<IFactoryCatalogDbContext>();
        db.SetupSet(x => x.UserFactories,
            new UserFactory { UserEmail = Email, FactoryId = "110" },
            new UserFactory { UserEmail = "other@tenengroup.com", FactoryId = "125" });
    }

    private CurrentUser CreateUser(params string[] roles)
    {
        roleResolver
            .Setup(r => r.Resolve(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleResolution(true, false, roles));

        return new CurrentUser(identity.Object, roleResolver.Object, toggle.Object, db.Object);
    }

    [Test]
    public async Task GetFactoryIds_ReturnsOnlyOwnFactories()
    {
        var factoryIds = await CreateUser(RoleNames.FactoryManager).GetFactoryIds();

        Assert.That(factoryIds, Is.EqualTo(new[] { "110" }));
    }

    [Test]
    public async Task CanEditFactory_OwnFactory_Allowed()
    {
        Assert.That(await CreateUser(RoleNames.FactoryManager).CanEditFactory("110"), Is.True);
    }

    [Test]
    public async Task CanEditFactory_OtherFactory_Denied()
    {
        Assert.That(await CreateUser(RoleNames.FactoryManager).CanEditFactory("125"), Is.False);
    }

    [Test]
    public async Task CanEditFactory_BoAdministrator_AnyFactory()
    {
        Assert.That(await CreateUser(RoleNames.BoAdministrator).CanEditFactory("315"), Is.True);
    }

    [Test]
    public async Task CanEditFactory_CatalogAdmin_IsNotAnOwner()
    {
        Assert.That(await CreateUser(RoleNames.CatalogAdmin).CanEditFactory("315"), Is.False);
    }

    [Test]
    public async Task CanEditFactory_ServiceAccount_AnyFactory()
    {
        identity.Setup(i => i.IsServiceAccount).Returns(true);

        Assert.That(await CreateUser().CanEditFactory("315"), Is.True);
    }

    [Test]
    public async Task CanEditFactory_EnforcementOff_AnyFactory()
    {
        toggle.Setup(t => t.IsEnforced(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.That(await CreateUser().CanEditFactory("315"), Is.True);
    }

    [Test]
    public void EnsureCanEditFactory_OtherFactory_ThrowsForbidden()
    {
        var user = CreateUser(RoleNames.FactoryManager);

        Assert.ThrowsAsync<ForbiddenException>(() => user.EnsureCanEditFactory("125"));
    }

    [Test]
    public void EnsureCanEditFactory_OwnFactory_DoesNotThrow()
    {
        var user = CreateUser(RoleNames.ShiftManager);

        Assert.DoesNotThrowAsync(() => user.EnsureCanEditFactory("110"));
    }

    [Test]
    public async Task HasPermission_FollowsRoles()
    {
        var user = CreateUser(RoleNames.Finance);

        Assert.That(await user.HasPermission(Permission.ComponentsViewCost), Is.True);
        Assert.That(await user.HasPermission(Permission.ProcessesEdit), Is.False);
    }

    [Test]
    public async Task GetRoles_ResolutionNotReady_NoRoles()
    {
        roleResolver
            .Setup(r => r.Resolve(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RoleResolution.NotReady);
        var user = new CurrentUser(identity.Object, roleResolver.Object, toggle.Object, db.Object);

        Assert.That(await user.GetRoles(), Is.Empty);
    }
}
