using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Users;

[TestFixture]
public class ListSettingsHandlersTests
{
    private const string Email = "user@tenengroup.com";
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<ICurrentUser> currentUser;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.Email).Returns(Email);
    }

    private UpdateListSettingsCommandHandler CreateUpdateHandler()
    {
        return new UpdateListSettingsCommandHandler(db.Object, currentUser.Object, TestDb.FixedTime(Now));
    }

    [Test]
    public async Task Update_NoSettingsYet_CreatesRow()
    {
        var rows = db.SetupSet(x => x.UserListSettings);

        var result = await CreateUpdateHandler().Handle(
            new UpdateListSettingsCommand("components", ["material", " unitCost ", "material"]), CancellationToken.None);

        Assert.That(result.Columns, Is.EqualTo(new[] { "material", "unitCost" }));
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].UserEmail, Is.EqualTo(Email));
        Assert.That(rows[0].ListKey, Is.EqualTo("components"));
        Assert.That(rows[0].UpdatedAt, Is.EqualTo(Now));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Update_ExistingSettings_ReplacesColumns()
    {
        var rows = db.SetupSet(x => x.UserListSettings,
            new UserListSettings { UserEmail = Email, ListKey = "components", Columns = ["material"] });

        await CreateUpdateHandler().Handle(new UpdateListSettingsCommand("components", ["family"]), CancellationToken.None);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].Columns, Is.EqualTo(new[] { "family" }));
    }

    [Test]
    public async Task Update_EightColumns_Accepted()
    {
        db.SetupSet(x => x.UserListSettings);
        var columns = Enumerable.Range(1, 8).Select(i => $"c{i}").ToArray();

        var result = await CreateUpdateHandler().Handle(new UpdateListSettingsCommand("skus", columns), CancellationToken.None);

        Assert.That(result.Columns, Has.Count.EqualTo(8));
    }

    [Test]
    public void Update_NineColumns_Rejected()
    {
        db.SetupSet(x => x.UserListSettings);
        var columns = Enumerable.Range(1, 9).Select(i => $"c{i}").ToArray();

        var exception = Assert.ThrowsAsync<ValidationException>(() =>
            CreateUpdateHandler().Handle(new UpdateListSettingsCommand("skus", columns), CancellationToken.None));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "columns" }));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Get_ReturnsOwnSettingsForTheList()
    {
        db.SetupSet(x => x.UserListSettings,
            new UserListSettings { UserEmail = Email, ListKey = "components", Columns = ["material"] },
            new UserListSettings { UserEmail = Email, ListKey = "skus", Columns = ["name"] },
            new UserListSettings { UserEmail = "other@tenengroup.com", ListKey = "components", Columns = ["family"] });

        var result = await new GetListSettingsQueryHandler(db.Object, currentUser.Object)
            .Handle(new GetListSettingsQuery("components"), CancellationToken.None);

        Assert.That(result.Columns, Is.EqualTo(new[] { "material" }));
    }

    [Test]
    public async Task Get_NothingSaved_EmptyColumns()
    {
        db.SetupSet(x => x.UserListSettings);

        var result = await new GetListSettingsQueryHandler(db.Object, currentUser.Object)
            .Handle(new GetListSettingsQuery("components"), CancellationToken.None);

        Assert.That(result.Columns, Is.Empty);
    }
}
