using System.ComponentModel.DataAnnotations;

namespace Tenengroup.FactoryCatalog.Api.Models.Skus;

public class SetSkuProducibleModel
{
    [Required]
    public bool? Producible { get; set; }
}

public class BulkSetSkuProducibleModel
{
    [Required]
    public string FactoryId { get; set; }

    [Required]
    [MinLength(1)]
    public Guid[] SkuUuids { get; set; }

    [Required]
    public bool? Producible { get; set; }
}

public class SetSkuShareOverrideModel
{
    [Required]
    [Range(0, 100)]
    public decimal? SharePct { get; set; }
}

public class UpdateGlobalFactoryShareModel
{
    [Required]
    [MinLength(1)]
    public List<FactoryShareModel> FactoryShares { get; set; }
}

public class FactoryShareModel
{
    [Required]
    public string FactoryId { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal? Pct { get; set; }
}
