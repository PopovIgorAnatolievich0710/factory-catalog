using System.ComponentModel.DataAnnotations;

namespace Tenengroup.FactoryCatalog.Api.Models.Shared;

public class UpdateListSettingsModel
{
    [Required]
    public string[] Columns { get; set; }
}
