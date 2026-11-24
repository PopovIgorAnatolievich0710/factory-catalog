using System.ComponentModel.DataAnnotations;

namespace Tenengroup.FactoryCatalog.Api.Models.Quotes;

public class MarkQuoteProducedModel
{
    [Required]
    public DateTimeOffset? ProducedAt { get; set; }

    public List<ProductionLogEntryModel> ProductionLog { get; set; } = [];

    public string ReproductionOfQuoteId { get; set; }
}

public class ProductionLogEntryModel
{
    [Required]
    public string ProcessId { get; set; }

    public string MachineType { get; set; }
}
