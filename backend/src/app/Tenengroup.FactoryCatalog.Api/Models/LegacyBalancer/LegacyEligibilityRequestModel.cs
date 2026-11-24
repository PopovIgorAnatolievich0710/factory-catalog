using System.ComponentModel.DataAnnotations;

namespace Tenengroup.FactoryCatalog.Api.Models.LegacyBalancer;

public class LegacyEligibilityRequestModel
{
    [Required]
    public string OrderId { get; set; }

    public string OrderItemId { get; set; }

    public string Sku { get; set; }

    public List<LegacyCbdItemModel> Cbd { get; set; } = [];

    [Required]
    [MinLength(1)]
    public List<string> FactoryIds { get; set; }
}

public class LegacyCbdItemModel
{
    [Required]
    public string ErpId { get; set; }

    public decimal? Quantity { get; set; }
}
