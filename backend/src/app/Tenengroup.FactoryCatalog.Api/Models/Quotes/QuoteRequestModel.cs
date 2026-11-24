using System.ComponentModel.DataAnnotations;
using Tenengroup.FactoryCatalog.Domains.Quotes;

namespace Tenengroup.FactoryCatalog.Api.Models.Quotes;

public class QuoteRequestModel
{
    [Required]
    public Guid? RequestId { get; set; }

    public string OrderId { get; set; }

    public string OrderItemId { get; set; }

    [Required]
    public string SkuUuid { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    public List<CbdItemModel> Cbd { get; set; }

    public QuoteMode Mode { get; set; } = QuoteMode.Live;
}

public class CbdItemModel
{
    [Required]
    public string ErpId { get; set; }

    public decimal Quantity { get; set; }
}
