using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.Business.Quotes;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Modules;

public static class QuotesModule
{
    public static IServiceCollection AddQuotesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(QuotesOptions.SectionName);
        var currency = section[nameof(QuotesOptions.Currency)];
        var options = new QuotesOptions(
            int.TryParse(section[nameof(QuotesOptions.ValidityMinutes)], out var validityMinutes) && validityMinutes > 0
                ? validityMinutes
                : QuotesOptions.DefaultValidityMinutes,
            string.IsNullOrWhiteSpace(currency) ? QuotesOptions.DefaultCurrency : currency.Trim().ToUpperInvariant());

        services.AddScoped(_ => options);

        services.AddScoped<IRequestHandler<RequestQuotesCommand, QuoteSetDto>, RequestQuotesCommandHandler>();
        services.AddScoped<IRequestHandler<GetQuoteSetQuery, QuoteSetDto>, GetQuoteSetQueryHandler>();
        services.AddScoped<IRequestHandler<SearchQuotesQuery, ListResult<QuoteDetailsDto>>, SearchQuotesQueryHandler>();
        services.AddScoped<IRequestHandler<SelectQuoteCommand, QuoteDetailsDto>, SelectQuoteCommandHandler>();
        services.AddScoped<IRequestHandler<MarkQuoteProducedCommand, QuoteDetailsDto>, MarkQuoteProducedCommandHandler>();
        services.AddScoped<IRequestHandler<MarkQuoteTransferredCommand, QuoteDetailsDto>, MarkQuoteTransferredCommandHandler>();
        services.AddScoped<IRequestHandler<GetDropshipperCostQuery, DropshipperCostDto>, GetDropshipperCostQueryHandler>();

        return services;
    }
}
