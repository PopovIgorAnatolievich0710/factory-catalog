using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.Business.Quotes;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Quotes;

[TestFixture]
public class QuoteLifecycleTests
{
    private const string QuoteId110 = "PQ-ORD456789-ITM001-110";
    private const string QuoteId125 = "PQ-ORD456789-ITM001-125";

    private static readonly DateTime SentAt = new(2026, 11, 27, 15, 20, 0, DateTimeKind.Utc);
    private static readonly ProductionLogEntryDto[] ProductionLog = [new("ENGRAVING", "Laser X2"), new("PACKING", null)];

    private CatalogFixture catalog;
    private QuoteSet set;

    [SetUp]
    public void SetUp()
    {
        catalog = new CatalogFixture();
        set = catalog.AddQuoteSet(1);
    }

    private Task<QuoteDetailsDto> Select(string quoteId)
    {
        return new SelectQuoteCommandHandler(catalog.Db.Object, catalog.CurrentUser.Object, catalog.Time)
            .Handle(new SelectQuoteCommand(quoteId), CancellationToken.None);
    }

    private Task<QuoteDetailsDto> Produced(string quoteId, DateTime? producedAt = null, string reproductionOf = null)
    {
        return new MarkQuoteProducedCommandHandler(catalog.Db.Object, catalog.CurrentUser.Object, catalog.Time)
            .Handle(new MarkQuoteProducedCommand(quoteId, producedAt ?? SentAt, ProductionLog, reproductionOf), CancellationToken.None);
    }

    private Task<QuoteDetailsDto> Transferred(string quoteId)
    {
        return new MarkQuoteTransferredCommandHandler(catalog.Db.Object, catalog.CurrentUser.Object, catalog.Time)
            .Handle(new MarkQuoteTransferredCommand(quoteId), CancellationToken.None);
    }

    private void VerifySaved(Times times)
    {
        catalog.Db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), times);
    }

    [Test]
    public async Task Select_QuotedQuote_BecomesSelected()
    {
        var quote = catalog.AddQuote(10, set, CatalogFixture.Factory110);
        catalog.AddQuote(11, set, CatalogFixture.Factory125);

        var result = await Select(QuoteId110);

        Assert.That(quote.Status, Is.EqualTo(QuoteStatus.Selected));
        Assert.That(quote.SelectedAt, Is.EqualTo(CatalogFixture.Now));
        Assert.That(result.Quote.Status, Is.EqualTo(QuoteStatus.Selected));
        Assert.That(result.QuoteKey, Is.EqualTo(set.QuoteKey));
        Assert.That(result.OrderId, Is.EqualTo("ORD456789"));
        Assert.That(result.Quote.Cost, Is.EqualTo(new QuoteCostDto(38.2m, 6.1m, 1.4m, 45.7m, "USD")));
        VerifySaved(Times.Once());
    }

    [Test]
    public async Task Select_AlreadySelected_IsIdempotent()
    {
        var selectedAt = CatalogFixture.Now.AddHours(-5);
        var quote = catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Selected, selectedAt: selectedAt);

        var result = await Select(QuoteId110);

        Assert.That(result.Quote.Status, Is.EqualTo(QuoteStatus.Selected));
        Assert.That(quote.SelectedAt, Is.EqualTo(selectedAt));
        VerifySaved(Times.Never());
    }

    [Test]
    public void Select_NotProducibleQuote_Conflicts()
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, producible: false);

        Assert.ThrowsAsync<ConflictException>(() => Select(QuoteId110));
        VerifySaved(Times.Never());
    }

    [TestCase(QuoteStatus.Selected)]
    [TestCase(QuoteStatus.Produced)]
    public void Select_WhenAnotherQuoteOfTheSetWon_Conflicts(QuoteStatus winnerStatus)
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110);
        catalog.AddQuote(11, set, CatalogFixture.Factory125, winnerStatus);

        Assert.ThrowsAsync<ConflictException>(() => Select(QuoteId110));
    }

    [Test]
    public async Task Select_AfterTheSiblingWasTransferred_IsAllowed()
    {
        var quote = catalog.AddQuote(10, set, CatalogFixture.Factory110);
        catalog.AddQuote(11, set, CatalogFixture.Factory125, QuoteStatus.Transferred);

        await Select(QuoteId110);

        Assert.That(quote.Status, Is.EqualTo(QuoteStatus.Selected));
    }

    [Test]
    public async Task Select_IgnoresWinnersOfOtherQuoteSets()
    {
        var otherSet = catalog.AddQuoteSet(2, orderItemId: "ITM002");
        catalog.AddQuote(20, otherSet, CatalogFixture.Factory125, QuoteStatus.Selected);
        var quote = catalog.AddQuote(10, set, CatalogFixture.Factory110);

        await Select(QuoteId110);

        Assert.That(quote.Status, Is.EqualTo(QuoteStatus.Selected));
    }

    [TestCase(QuoteStatus.Produced)]
    [TestCase(QuoteStatus.Transferred)]
    public void Select_ProducedOrTransferredQuote_Conflicts(QuoteStatus status)
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, status);

        Assert.ThrowsAsync<ConflictException>(() => Select(QuoteId110));
    }

    [Test]
    public void UnknownQuote_NotFound()
    {
        Assert.ThrowsAsync<EntityNotFoundException>(() => Select("PQ-NONE"));
        Assert.ThrowsAsync<EntityNotFoundException>(() => Produced("PQ-NONE"));
        Assert.ThrowsAsync<EntityNotFoundException>(() => Transferred("PQ-NONE"));
    }

    [Test]
    public void UiUser_CannotDriveTheLifecycle()
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Selected);
        catalog.CurrentUser.Setup(u => u.IsServiceAccount).Returns(false);

        Assert.ThrowsAsync<ForbiddenException>(() => Select(QuoteId110));
        Assert.ThrowsAsync<ForbiddenException>(() => Produced(QuoteId110));
        Assert.ThrowsAsync<ForbiddenException>(() => Transferred(QuoteId110));
    }

    [Test]
    public async Task Produced_SelectedQuote_BecomesProducedWithTheProductionLog()
    {
        var quote = catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Selected);

        var result = await Produced(QuoteId110);

        Assert.That(quote.Status, Is.EqualTo(QuoteStatus.Produced));
        Assert.That(quote.ProducedAt, Is.EqualTo(SentAt));
        Assert.That(quote.ReproductionOfQuoteId, Is.Null);
        Assert.That(result.Quote.Status, Is.EqualTo(QuoteStatus.Produced));
        Assert.That(result.Quote.ProducedAt, Is.EqualTo(SentAt));
        Assert.That(result.Quote.ProductionLog, Is.EqualTo(ProductionLog));
        Assert.That(result.Quote.ReproductionOf, Is.Null);
        VerifySaved(Times.Once());
    }

    [Test]
    public async Task Produced_SameSentEventAgain_IsIdempotent()
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Selected);
        await Produced(QuoteId110);

        var result = await Produced(QuoteId110);

        Assert.That(result.Quote.Status, Is.EqualTo(QuoteStatus.Produced));
        Assert.That(catalog.Quotes, Has.Count.EqualTo(1));
        VerifySaved(Times.Once());
    }

    [Test]
    public async Task Produced_AgainWithAnotherDateAndNoReproductionLink_Conflicts()
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Selected);
        await Produced(QuoteId110);

        Assert.ThrowsAsync<ConflictException>(() => Produced(QuoteId110, SentAt.AddDays(2)));
    }

    [TestCase(QuoteStatus.Quoted)]
    [TestCase(QuoteStatus.Transferred)]
    public void Produced_QuoteThatIsNotSelected_Conflicts(QuoteStatus status)
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, status);

        Assert.ThrowsAsync<ConflictException>(() => Produced(QuoteId110));
        VerifySaved(Times.Never());
    }

    [Test]
    public void Produced_WithoutProducedAt_IsRejected()
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Selected);

        var exception = Assert.ThrowsAsync<ValidationException>(() => Produced(QuoteId110, default(DateTime)));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "producedAt" }));
    }

    [Test]
    public async Task Reproduction_CreatesANewProducedRecordLinkedToTheOriginal()
    {
        var original = catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: SentAt);
        var reproducedAt = SentAt.AddDays(3);

        var result = await Produced(QuoteId110, reproducedAt, reproductionOf: QuoteId110);

        Assert.That(catalog.Quotes, Has.Count.EqualTo(2));
        var reproduction = catalog.Quotes[1];
        Assert.That(reproduction.QuoteId, Is.EqualTo("PQ-ORD456789-ITM001-110-RP1"));
        Assert.That(reproduction.Status, Is.EqualTo(QuoteStatus.Produced));
        Assert.That(reproduction.ProducedAt, Is.EqualTo(reproducedAt));
        Assert.That(reproduction.ReproductionOfQuoteId, Is.EqualTo(10));
        Assert.That(reproduction.QuoteSetId, Is.EqualTo(set.Id));
        Assert.That(reproduction.FactoryId, Is.EqualTo(CatalogFixture.Factory110));
        Assert.That(reproduction.TotalCost, Is.EqualTo(45.7m));
        Assert.That(reproduction.CreatedAt, Is.EqualTo(CatalogFixture.Now));
        Assert.That(original.ProducedAt, Is.EqualTo(SentAt));
        Assert.That(original.ReproductionOfQuoteId, Is.Null);
        Assert.That(result.Quote.QuoteId, Is.EqualTo("PQ-ORD456789-ITM001-110-RP1"));
        Assert.That(result.Quote.ReproductionOf, Is.EqualTo(QuoteId110));
        Assert.That(result.Quote.ProductionLog, Is.EqualTo(ProductionLog));
    }

    [Test]
    public async Task Reproduction_SameEventAgain_IsIdempotent()
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: SentAt);
        var reproducedAt = SentAt.AddDays(3);
        await Produced(QuoteId110, reproducedAt, reproductionOf: QuoteId110);
        catalog.Quotes[1].Id = 11;

        var result = await Produced(QuoteId110, reproducedAt, reproductionOf: QuoteId110);

        Assert.That(catalog.Quotes, Has.Count.EqualTo(2));
        Assert.That(result.Quote.QuoteId, Is.EqualTo("PQ-ORD456789-ITM001-110-RP1"));
        VerifySaved(Times.Once());
    }

    [Test]
    public async Task Reproduction_SecondOne_GetsItsOwnRecord()
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: SentAt);
        await Produced(QuoteId110, SentAt.AddDays(3), reproductionOf: QuoteId110);
        catalog.Quotes[1].Id = 11;

        var result = await Produced(QuoteId110, SentAt.AddDays(6), reproductionOf: QuoteId110);

        Assert.That(catalog.Quotes, Has.Count.EqualTo(3));
        Assert.That(result.Quote.QuoteId, Is.EqualTo("PQ-ORD456789-ITM001-110-RP2"));
        Assert.That(catalog.Quotes[2].ReproductionOfQuoteId, Is.EqualTo(10));
    }

    [Test]
    public async Task Reproduction_UnderAnotherSelectedQuote_MarksThatQuoteProducedAndLinksIt()
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: SentAt);
        var requote = catalog.AddQuoteSet(2);
        requote.QuoteKey = "PQ-ORD456789-ITM001-R2";
        var reproducedElsewhere = catalog.AddQuote(20, requote, CatalogFixture.Factory125, QuoteStatus.Selected);

        var result = await Produced(reproducedElsewhere.QuoteId, SentAt.AddDays(3), reproductionOf: QuoteId110);

        Assert.That(catalog.Quotes, Has.Count.EqualTo(2));
        Assert.That(reproducedElsewhere.Status, Is.EqualTo(QuoteStatus.Produced));
        Assert.That(reproducedElsewhere.ReproductionOfQuoteId, Is.EqualTo(10));
        Assert.That(result.Quote.QuoteId, Is.EqualTo("PQ-ORD456789-ITM001-R2-125"));
        Assert.That(result.Quote.ReproductionOf, Is.EqualTo(QuoteId110));
    }

    [Test]
    public void Reproduction_OfAQuoteThatWasNeverProduced_Conflicts()
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Selected);

        Assert.ThrowsAsync<ConflictException>(() => Produced(QuoteId110, reproductionOf: QuoteId110));
        Assert.That(catalog.Quotes, Has.Count.EqualTo(1));
    }

    [Test]
    public void Reproduction_OfAnUnknownQuote_NotFound()
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: SentAt);

        Assert.ThrowsAsync<EntityNotFoundException>(() => Produced(QuoteId110, reproductionOf: "PQ-NONE"));
    }

    [Test]
    public void Reproduction_UnderAQuoteThatWasNeverSelected_Conflicts()
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: SentAt);
        catalog.AddQuote(11, set, CatalogFixture.Factory125);

        Assert.ThrowsAsync<ConflictException>(() => Produced(QuoteId125, SentAt.AddDays(3), reproductionOf: QuoteId110));
    }

    [Test]
    public async Task Transferred_SelectedQuote_BecomesTransferred()
    {
        var quote = catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Selected);

        var result = await Transferred(QuoteId110);

        Assert.That(quote.Status, Is.EqualTo(QuoteStatus.Transferred));
        Assert.That(quote.TransferredAt, Is.EqualTo(CatalogFixture.Now));
        Assert.That(result.Quote.Status, Is.EqualTo(QuoteStatus.Transferred));
        Assert.That(result.Quote.TransferredAt, Is.EqualTo(CatalogFixture.Now));
        VerifySaved(Times.Once());
    }

    [Test]
    public async Task Transferred_Again_IsIdempotent()
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Selected);
        await Transferred(QuoteId110);

        var result = await Transferred(QuoteId110);

        Assert.That(result.Quote.Status, Is.EqualTo(QuoteStatus.Transferred));
        VerifySaved(Times.Once());
    }

    [TestCase(QuoteStatus.Quoted)]
    [TestCase(QuoteStatus.Produced)]
    public void Transferred_QuoteThatIsNotSelected_Conflicts(QuoteStatus status)
    {
        catalog.AddQuote(10, set, CatalogFixture.Factory110, status);

        Assert.ThrowsAsync<ConflictException>(() => Transferred(QuoteId110));
        VerifySaved(Times.Never());
    }
}
