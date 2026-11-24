namespace Tenengroup.FactoryCatalog.Domains.Quotes;

public class Quote
{
    public long Id { get; set; }

    public string QuoteId { get; set; } = string.Empty;

    public long QuoteSetId { get; set; }

    public string FactoryId { get; set; } = string.Empty;

    public bool Producible { get; set; }

    public List<string> Reasons { get; set; } = [];

    public decimal? MaterialCost { get; set; }

    public decimal? LaborCost { get; set; }

    public decimal? PackingCost { get; set; }

    public decimal? TotalCost { get; set; }

    public string? Currency { get; set; }

    public int? CapacitySignalPct { get; set; }

    public decimal SharePct { get; set; }

    public string ComponentsJson { get; set; } = "[]";

    public string ProcessesJson { get; set; } = "[]";

    public QuoteStatus Status { get; set; }

    public DateTime? SelectedAt { get; set; }

    public DateTime? ProducedAt { get; set; }

    public DateTime? TransferredAt { get; set; }

    public long? ReproductionOfQuoteId { get; set; }

    public string? ProductionLogJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public QuoteSet? QuoteSet { get; set; }
}
