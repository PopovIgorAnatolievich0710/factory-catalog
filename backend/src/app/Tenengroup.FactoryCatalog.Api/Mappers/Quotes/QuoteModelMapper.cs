using Tenengroup.FactoryCatalog.Api.Models.Quotes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;

namespace Tenengroup.FactoryCatalog.Api.Mappers.Quotes;

public static class QuoteModelMapper
{
    public static RequestQuotesCommand ToCommand(this QuoteRequestModel model)
    {
        return new RequestQuotesCommand(
            model.RequestId.GetValueOrDefault(),
            model.OrderId,
            model.OrderItemId,
            model.SkuUuid,
            model.Quantity,
            model.Cbd.Select(c => new CbdItem(c.ErpId, c.Quantity)).ToList(),
            model.Mode);
    }

    public static MarkQuoteProducedCommand ToCommand(this MarkQuoteProducedModel model, string quoteId)
    {
        return new MarkQuoteProducedCommand(
            quoteId,
            model.ProducedAt.GetValueOrDefault().UtcDateTime,
            (model.ProductionLog ?? []).Select(l => new ProductionLogEntryDto(l.ProcessId, l.MachineType)).ToList(),
            model.ReproductionOfQuoteId);
    }
}
