using System.ComponentModel.DataAnnotations;

namespace Tenengroup.FactoryCatalog.Api.Models.Components;

public class SetManualOosModel
{
    public bool Active { get; set; }

    [MaxLength(1000)]
    public string Note { get; set; }
}
