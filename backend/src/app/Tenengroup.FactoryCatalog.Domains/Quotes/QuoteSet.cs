namespace Tenengroup.FactoryCatalog.Domains.Quotes;

public class QuoteSet
{
    public long Id { get; set; }

    public string QuoteKey { get; set; } = string.Empty;

    public Guid RequestId { get; set; }

    public string OrderId { get; set; } = string.Empty;

    public string OrderItemId { get; set; } = string.Empty;

    public Guid SkuUuid { get; set; }

    public int Quantity { get; set; }

    public QuoteMode Mode { get; set; }

    public string CbdJson { get; set; } = "[]";

    public DateTime ValidUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<Quote> Quotes { get; set; } = [];
}
