using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Alternatives;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Components.Alternatives;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Components;

[TestFixture]
public class AlternativeCommandHandlersTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<ICurrentUser> currentUser;
    private Mock<IHistoryWriter> history;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        currentUser = new Mock<ICurrentUser>();
        history = new Mock<IHistoryWriter>();
        currentUser.Setup(u => u.Email).Returns("sc@tenengroup.com");
        db.SetupSet(x => x.Components,
            new Component { ErpId = "A", Name = "Alpha", Status = ComponentStatus.Active },
            new Component { ErpId = "B", Name = "Beta", Status = ComponentStatus.Active },
            new Component { ErpId = "OLD", Name = "Old", Status = ComponentStatus.Inactive });
    }

    private Task Add(string erpId, string alternativeErpId)
    {
        return new AddAlternativeCommandHandler(db.Object, currentUser.Object, history.Object, TestDb.FixedTime(Now))
            .Handle(new AddAlternativeCommand(erpId, alternativeErpId, true, false), CancellationToken.None);
    }

    [Test]
    public async Task Add_ActiveComponent_AddsOneWayLink()
    {
        var alternatives = db.SetupSet(x => x.ComponentAlternatives);

        await Add("A", "B");

        Assert.That(alternatives, Has.Count.EqualTo(1));
        Assert.That(alternatives[0].ComponentErpId, Is.EqualTo("A"));
        Assert.That(alternatives[0].AlternativeErpId, Is.EqualTo("B"));
        Assert.That(alternatives[0].ForBalancer, Is.True);
        Assert.That(alternatives[0].ForPlanner, Is.False);
        Assert.That(alternatives[0].CreatedBy, Is.EqualTo("sc@tenengroup.com"));
        Assert.That(alternatives[0].CreatedAt, Is.EqualTo(Now));
        history.Verify(h => h.Record(HistoryEntityTypes.Component, "A", "alternative", null, "B", HistorySource.User, null, null), Times.Once);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Add_ReverseDirectionOfExistingLink_IsAllowed()
    {
        var alternatives = db.SetupSet(x => x.ComponentAlternatives,
            new ComponentAlternative { ComponentErpId = "B", AlternativeErpId = "A" });

        await Add("A", "B");

        Assert.That(alternatives, Has.Count.EqualTo(2));
    }

    [TestCase("A", "A")]
    [TestCase("A", "OLD")]
    [TestCase("A", "MISSING")]
    public void Add_ItselfInactiveOrUnknown_Rejected(string erpId, string alternativeErpId)
    {
        var alternatives = db.SetupSet(x => x.ComponentAlternatives);

        var exception = Assert.ThrowsAsync<ValidationException>(() => Add(erpId, alternativeErpId));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "alternativeErpId" }));
        Assert.That(alternatives, Is.Empty);
    }

    [Test]
    public void Add_Duplicate_Conflict()
    {
        db.SetupSet(x => x.ComponentAlternatives, new ComponentAlternative { ComponentErpId = "A", AlternativeErpId = "B" });

        Assert.ThrowsAsync<ConflictException>(() => Add("A", "B"));
    }

    [Test]
    public void Add_UnknownComponent_NotFound()
    {
        db.SetupSet(x => x.ComponentAlternatives);

        Assert.ThrowsAsync<EntityNotFoundException>(() => Add("MISSING", "B"));
    }

    [Test]
    public async Task SetUse_UpdatesBothFlagsAndWritesHistory()
    {
        var alternative = new ComponentAlternative { ComponentErpId = "A", AlternativeErpId = "B", ForBalancer = true, ForPlanner = false };
        db.SetupSet(x => x.ComponentAlternatives, alternative);

        await new SetAlternativeUseCommandHandler(db.Object, history.Object, TestDb.FixedTime(Now))
            .Handle(new SetAlternativeUseCommand("A", "B", false, true), CancellationToken.None);

        Assert.That(alternative.ForBalancer, Is.False);
        Assert.That(alternative.ForPlanner, Is.True);
        Assert.That(alternative.UpdatedAt, Is.EqualTo(Now));
        history.Verify(h => h.Record(HistoryEntityTypes.Component, "A", AlternativeHistoryFields.ForBalancer("B"), true, false, HistorySource.User, null, null), Times.Once);
        history.Verify(h => h.Record(HistoryEntityTypes.Component, "A", AlternativeHistoryFields.ForPlanner("B"), false, true, HistorySource.User, null, null), Times.Once);
    }

    [Test]
    public async Task Remove_DeletesTheLink()
    {
        var alternative = new ComponentAlternative { ComponentErpId = "A", AlternativeErpId = "B" };
        db.SetupSet(x => x.ComponentAlternatives, alternative);

        await new RemoveAlternativeCommandHandler(db.Object, history.Object)
            .Handle(new RemoveAlternativeCommand("A", "B"), CancellationToken.None);

        Mock.Get(db.Object.ComponentAlternatives).Verify(s => s.Remove(alternative), Times.Once);
        history.Verify(h => h.Record(HistoryEntityTypes.Component, "A", "alternative", "B", null, HistorySource.User, null, null), Times.Once);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Remove_UnknownLink_NotFound()
    {
        db.SetupSet(x => x.ComponentAlternatives);

        Assert.ThrowsAsync<EntityNotFoundException>(() =>
            new RemoveAlternativeCommandHandler(db.Object, history.Object)
                .Handle(new RemoveAlternativeCommand("A", "B"), CancellationToken.None));
    }
}
