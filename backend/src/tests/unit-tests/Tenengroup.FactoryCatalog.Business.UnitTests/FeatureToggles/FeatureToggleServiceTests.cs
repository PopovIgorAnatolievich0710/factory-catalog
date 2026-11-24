using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Shared.FeatureToggles;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.FeatureToggles;

[TestFixture]
public class FeatureToggleServiceTests
{
    private static FeatureToggleService CreateService(params FeatureToggle[] toggles)
    {
        var db = new Mock<IFactoryCatalogDbContext>();
        db.SetupSet(x => x.FeatureToggles, toggles);

        return new FeatureToggleService(db.Object, new MemoryCache(new MemoryCacheOptions()));
    }

    [Test]
    public async Task IsEnforced_NoRow_False()
    {
        Assert.That(await CreateService().IsEnforced(), Is.False);
    }

    [Test]
    public async Task IsEnforced_DisabledRow_False()
    {
        var service = CreateService(new FeatureToggle { Name = FeatureToggleNames.PermissionsEnableAll, IsEnabled = false });

        Assert.That(await service.IsEnforced(), Is.False);
    }

    [Test]
    public async Task IsEnforced_EnabledRow_True()
    {
        var service = CreateService(new FeatureToggle { Name = FeatureToggleNames.PermissionsEnableAll, IsEnabled = true });

        Assert.That(await service.IsEnforced(), Is.True);
    }

    [Test]
    public async Task GetAll_SortedByName()
    {
        var service = CreateService(new FeatureToggle { Name = "b" }, new FeatureToggle { Name = "a" });

        Assert.That((await service.GetAll()).Select(t => t.Name), Is.EqualTo(new[] { "a", "b" }));
    }
}
