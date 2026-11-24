using System.ComponentModel.DataAnnotations;

namespace Tenengroup.FactoryCatalog.Api.Models.Components;

public class RecordComponentReplacementModel
{
    [Required]
    [MaxLength(100)]
    public string OrderId { get; set; }

    [Required]
    [MaxLength(100)]
    public string OrderItemId { get; set; }

    [Required]
    [MaxLength(20)]
    public string FactoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public string OriginalErpId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ReplacementErpId { get; set; }

    public decimal? Quantity { get; set; }

    [Required]
    [MaxLength(320)]
    public string ReplacedBy { get; set; }

    public DateTime? ReplacedAt { get; set; }
}
