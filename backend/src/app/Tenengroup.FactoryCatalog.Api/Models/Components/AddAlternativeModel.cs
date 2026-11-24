using System.ComponentModel.DataAnnotations;

namespace Tenengroup.FactoryCatalog.Api.Models.Components;

public class AddAlternativeModel
{
    [Required]
    [MaxLength(100)]
    public string AlternativeErpId { get; set; }

    public bool ForBalancer { get; set; }

    public bool ForPlanner { get; set; }
}
