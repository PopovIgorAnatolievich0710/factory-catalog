using System.ComponentModel.DataAnnotations;

namespace Tenengroup.FactoryCatalog.Api.Models.Components;

public class SetCriticalLevelModel
{
    [Range(0, 99999999999999)]
    public decimal? Level { get; set; }
}
