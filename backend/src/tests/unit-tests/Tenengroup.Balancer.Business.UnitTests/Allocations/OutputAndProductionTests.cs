using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Tenengroup.Balancer.Business.Abstractions;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Decisions;
using Tenengroup.Balancer.Business.Abstractions.Events;
using Tenengroup.Balancer.Business.Abstractions.Production;
using Tenengroup.Balancer.Business.Allocations;
using Tenengroup.Balancer.Business.Production;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Aws.Sns;
using Tenengroup.Infrastructure.Common.Json;
using static Tenengroup.Balancer.Business.UnitTests.TestData;

namespace Tenengroup.Balancer.Business.UnitTests.Allocations;

[TestFixture]
public class OutputAndProductionTests
{
    private const string TopicArn = "arn:aws:sns:eu-west-1:000000000000:balancer-allocation-decided";
    private const string ProducedTopicArn = "arn:aws:sns:eu-west-1:000000000000:balancer-quote-produced";

    private Mock<IBalancerDbContext> db;
    private Mock<ICatalogClient> catalog;
    private Mock<IEventPublisher> publisher;
    private BalancerEventsOptions options;

    [SetUp]
    public void SetUp()
    {
        db = EmptyDb();
        catalog = new Mock<ICatalogClient>();
        publisher = new Mock<IEventPublisher>();
        options = new BalancerEventsOptions { AllocationDecidedTopicArn = TopicArn, QuoteProducedTopicArn = ProducedTopicArn };

        catalog
            .Setup(c => c.MarkQuoteProduced(It.IsAny<string>(), It.IsAny<MarkQuoteProducedRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string quoteId, MarkQuoteProducedRequest request, CancellationToken _) => new QuoteDetails(
                "PQ-O1-I1", Guid.Empty, "O1", "I1", "SKU-1", 1, "Live", Now.AddMinutes(15), Now,
                Quote("110", 45m) with { QuoteId = request.ReproductionOfQuoteId == null ? quoteId : $"{quoteId}-RP1" }));
    }

    private AllocationOutputJob OutputJob()
    {
        return new AllocationOutputJob(db.Object, catalog.Object, publisher.Object, options, FixedTime(), NullLogger<AllocationOutputJob>.Instance);
    }

    private RecordProductionCommandHandler ProductionHandler()
    {
        return new RecordProductionCommandHandler(
            db.Object, catalog.Object, publisher.Object, options, FixedTime(), NullLogger<RecordProductionCommandHandler>.Instance);
    }

    [Test]
    public async Task Output_marks_the_winning_quote_selected_and_publishes_the_allocation_event()
    {
        var allocation = OpenAllocation();
        allocation.CandidatesJson = new[]
        {
            new Candidate("110", "PQ-O1-I1-110", 45m, 10m, 55m, "USD", Today.AddDays(6), Today.AddDays(8), 5, 100m, 100, 100m, 52m, true, null, null, true),
            new Candidate("125", "PQ-O1-I1-125", null, null, null, null, null, null, null, 0m, null, null, null, null, null, "COMPONENT_OOS", false)
        }.ToJsonString();
        db.SetupSet(x => x.Allocations, allocation);
        AllocationDecidedEvent published = null;
        publisher
            .Setup(p => p.Publish(TopicArn, BalancerEventTypes.AllocationDecided, It.IsAny<AllocationDecidedEvent>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string _, AllocationDecidedEvent e, CancellationToken _) => published = e)
            .ReturnsAsync("message-id");

        await OutputJob().Publish(allocation.Id, CancellationToken.None);

        catalog.Verify(c => c.SelectQuote("PQ-O1-I1-110", It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(published, Is.Not.Null);
        Assert.That(published.OrderId, Is.EqualTo("O1"));
        Assert.That(published.OrderItemId, Is.EqualTo("I1"));
        Assert.That(published.QuoteKey, Is.EqualTo("PQ-O1-I1"));
        Assert.That(published.SelectedQuoteId, Is.EqualTo("PQ-O1-I1-110"));
        Assert.That(published.FactoryId, Is.EqualTo("110"));
        Assert.That(published.PackingFacilityId, Is.EqualTo("110"));
        Assert.That(published.ProductionCost, Is.EqualTo(new Money(45m, "USD")));
        Assert.That(published.ShippingCost, Is.EqualTo(new Money(10m, "USD")));
        Assert.That(published.TotalCost, Is.EqualTo(new Money(55m, "USD")));
        Assert.That(published.Eta, Is.EqualTo(new EtaRange(Today.AddDays(6), Today.AddDays(8))));
        Assert.That(published.Mode, Is.EqualTo("live"));
        Assert.That(published.Candidates.Single(c => c.FactoryId == "125").ExcludedReason, Is.EqualTo("COMPONENT_OOS"));
        Assert.That(published.Candidates.Single(c => c.FactoryId == "110").TotalCost, Is.EqualTo(55m));
        Assert.That(allocation.OutputPublishedAt, Is.EqualTo(Now));
    }

    [Test]
    public async Task Output_is_published_once()
    {
        var allocation = OpenAllocation();
        allocation.OutputPublishedAt = Now.AddMinutes(-1);
        db.SetupSet(x => x.Allocations, allocation);

        await OutputJob().Publish(allocation.Id, CancellationToken.None);

        catalog.VerifyNoOtherCalls();
        publisher.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Shadow_allocations_are_never_acted_on()
    {
        var allocation = OpenAllocation();
        allocation.Mode = AllocationMode.Shadow;
        db.SetupSet(x => x.Allocations, allocation);

        await OutputJob().Publish(allocation.Id, CancellationToken.None);

        catalog.VerifyNoOtherCalls();
        publisher.VerifyNoOtherCalls();
    }

    [Test]
    public async Task A_fallback_allocation_without_a_quote_still_emits_the_event_with_fallback_mode()
    {
        var allocation = OpenAllocation();
        allocation.Mode = AllocationMode.Fallback;
        allocation.SelectedQuoteId = null;
        db.SetupSet(x => x.Allocations, allocation);

        await OutputJob().Publish(allocation.Id, CancellationToken.None);

        catalog.Verify(c => c.SelectQuote(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(p => p.Publish(
            TopicArn,
            BalancerEventTypes.AllocationDecided,
            It.Is<AllocationDecidedEvent>(e => e.Mode == "fallback"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Sent_event_marks_the_quote_produced_stores_the_record_and_fires_the_produced_event()
    {
        var allocation = OpenAllocation();
        allocation.PaidAt = Now.AddDays(-4);
        db.SetupSet(x => x.Allocations, allocation);
        db.SetupSet(x => x.AllocationCostLines,
            new AllocationCostLine { AllocationId = allocation.Id, Kind = CostLineKind.Process, Code = "ENGRAVING", Quantity = 2m, QuotedUnitCost = 3m, QuotedCost = 6m },
            new AllocationCostLine { AllocationId = allocation.Id, Kind = CostLineKind.Packing, Code = "PACKING", Quantity = 1m, QuotedUnitCost = 1m, QuotedCost = 1m });
        QuoteProducedEvent published = null;
        publisher
            .Setup(p => p.Publish(ProducedTopicArn, BalancerEventTypes.QuoteProduced, It.IsAny<QuoteProducedEvent>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string _, QuoteProducedEvent e, CancellationToken _) => published = e)
            .ReturnsAsync("message-id");
        var log = new[] { new SupplierProductionLogEntry("ENGRAVING", "Laser X2", 3m) };

        var handled = await ProductionHandler().Handle(
            new RecordProductionCommand("evt-1", "O1", "I1", Now, log), CancellationToken.None);

        Assert.That(handled, Is.True);
        catalog.Verify(c => c.MarkQuoteProduced(
            "PQ-O1-I1-110",
            It.Is<MarkQuoteProducedRequest>(r => r.ProducedAt == Now && r.ReproductionOfQuoteId == null && r.ProductionLog.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);

        var record = db.Object.ProductionRecords.Single();
        Assert.That(record.QuoteId, Is.EqualTo("PQ-O1-I1-110"));
        Assert.That(record.IsReproduction, Is.False);
        Assert.That(record.ProductionDays, Is.EqualTo(4));
        Assert.That(record.SourceEventId, Is.EqualTo("evt-1"));

        Assert.That(allocation.Status, Is.EqualTo(AllocationStatus.Produced));
        Assert.That(allocation.ProducedAt, Is.EqualTo(Now));
        Assert.That(allocation.ActualLaborCost, Is.EqualTo(9m));

        Assert.That(published.Status, Is.EqualTo("produced"));
        Assert.That(published.QuoteId, Is.EqualTo("PQ-O1-I1-110"));
        Assert.That(published.QuoteKey, Is.EqualTo("PQ-O1-I1"));
        Assert.That(published.FactoryId, Is.EqualTo("110"));
        Assert.That(published.QuotedCost, Is.EqualTo(new QuoteCost(38m, 6m, 1m, 45m, "USD")));
        Assert.That(published.SentAt, Is.EqualTo(Now));
        Assert.That(published.ProductionDays, Is.EqualTo(4));
        Assert.That(published.ReproductionOf, Is.Null);
    }

    [Test]
    public async Task A_sending_after_a_reproduction_request_creates_a_new_record_linked_to_the_original()
    {
        var allocation = OpenAllocation();
        allocation.Status = AllocationStatus.Produced;
        allocation.ProducedAt = Now.AddDays(-2);
        allocation.ReproductionRequestedAt = Now.AddDays(-1);
        db.SetupSet(x => x.Allocations, allocation);
        db.SetupSet(x => x.ProductionRecords, new ProductionRecord
        {
            Id = 7,
            AllocationId = allocation.Id,
            SourceEventId = "evt-1",
            QuoteId = "PQ-O1-I1-110",
            QuoteKey = "PQ-O1-I1",
            FactoryId = "110",
            SentAt = Now.AddDays(-2)
        });
        QuoteProducedEvent published = null;
        publisher
            .Setup(p => p.Publish(ProducedTopicArn, BalancerEventTypes.QuoteProduced, It.IsAny<QuoteProducedEvent>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string _, QuoteProducedEvent e, CancellationToken _) => published = e)
            .ReturnsAsync("message-id");

        var handled = await ProductionHandler().Handle(
            new RecordProductionCommand("evt-2", "O1", "I1", Now, []), CancellationToken.None);

        Assert.That(handled, Is.True);
        catalog.Verify(c => c.MarkQuoteProduced(
            "PQ-O1-I1-110",
            It.Is<MarkQuoteProducedRequest>(r => r.ReproductionOfQuoteId == "PQ-O1-I1-110"),
            It.IsAny<CancellationToken>()), Times.Once);

        var records = db.Object.ProductionRecords.ToList();
        Assert.That(records, Has.Count.EqualTo(2));
        var reproduction = records.Single(r => r.IsReproduction);
        Assert.That(reproduction.QuoteId, Is.EqualTo("PQ-O1-I1-110-RP1"));
        Assert.That(reproduction.ReproductionOfQuoteId, Is.EqualTo("PQ-O1-I1-110"));
        Assert.That(reproduction.ReproductionOfRecordId, Is.EqualTo(7));
        Assert.That(allocation.ProducedAt, Is.EqualTo(Now.AddDays(-2)));
        Assert.That(published.ReproductionOf, Is.EqualTo("PQ-O1-I1-110"));
        Assert.That(published.QuoteId, Is.EqualTo("PQ-O1-I1-110-RP1"));
    }

    [Test]
    public async Task A_repeated_sending_without_a_reproduction_request_is_ignored()
    {
        var allocation = OpenAllocation();
        db.SetupSet(x => x.Allocations, allocation);
        db.SetupSet(x => x.ProductionRecords, new ProductionRecord
        {
            AllocationId = allocation.Id,
            SourceEventId = "evt-1",
            QuoteId = "PQ-O1-I1-110",
            SentAt = Now.AddMinutes(-5)
        });

        var handled = await ProductionHandler().Handle(
            new RecordProductionCommand("evt-2", "O1", "I1", Now, []), CancellationToken.None);

        Assert.That(handled, Is.True);
        Assert.That(db.Object.ProductionRecords.Count(), Is.EqualTo(1));
        catalog.VerifyNoOtherCalls();
    }

    [Test]
    public async Task A_reproduction_status_marks_the_live_allocation()
    {
        var allocation = OpenAllocation();
        db.SetupSet(x => x.Allocations, allocation);

        var handled = await new MarkReproductionRequestedCommandHandler(db.Object, NullLogger<MarkReproductionRequestedCommandHandler>.Instance)
            .Handle(new MarkReproductionRequestedCommand("O1", "I1", Now), CancellationToken.None);

        Assert.That(handled, Is.True);
        Assert.That(allocation.ReproductionRequestedAt, Is.EqualTo(Now));
    }

    [Test]
    public async Task The_same_event_is_processed_once()
    {
        var allocation = OpenAllocation();
        db.SetupSet(x => x.Allocations, allocation);
        db.SetupSet(x => x.ProductionRecords, new ProductionRecord { AllocationId = allocation.Id, SourceEventId = "evt-1" });

        var handled = await ProductionHandler().Handle(
            new RecordProductionCommand("evt-1", "O1", "I1", Now, []), CancellationToken.None);

        Assert.That(handled, Is.True);
        catalog.VerifyNoOtherCalls();
        publisher.VerifyNoOtherCalls();
    }

    [Test]
    public async Task An_event_without_a_live_allocation_is_dropped()
    {
        var shadow = OpenAllocation();
        shadow.Mode = AllocationMode.Shadow;
        db.SetupSet(x => x.Allocations, shadow);

        var handled = await ProductionHandler().Handle(
            new RecordProductionCommand("evt-1", "O1", "I1", Now, []), CancellationToken.None);

        Assert.That(handled, Is.True);
        catalog.VerifyNoOtherCalls();
    }
}
