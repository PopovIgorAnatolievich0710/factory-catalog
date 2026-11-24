namespace Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;

public record QuotesOptions(int ValidityMinutes, string Currency)
{
    public const string SectionName = "Quotes";
    public const int DefaultValidityMinutes = 15;
    public const string DefaultCurrency = "USD";
}
