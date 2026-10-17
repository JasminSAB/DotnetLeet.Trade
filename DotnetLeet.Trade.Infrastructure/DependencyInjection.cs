using DotnetLeet.Trade.Application.Procurement.ExtractOffer;
using DotnetLeet.Trade.Application.Procurement.Ports;
using DotnetLeet.Trade.Application.Procurement.Queries;
using DotnetLeet.Trade.Application.Procurement.ReviewOffer;
using DotnetLeet.Trade.Infrastructure.Ai;
using DotnetLeet.Trade.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetLeet.Trade.Infrastructure;

/// <summary>
/// Wires ports to adapters. The host registers the DbContext and the IChatClient
/// (Aspire does both), this method registers everything that depends on them.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddTrade(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.Configure<ExtractionOptions>(configuration.GetSection(ExtractionOptions.Section));

        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPurchasePriceHistoryRepository, PurchasePriceHistoryRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IAiCallLog, EfAiCallLog>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddScoped<IOfferExtractor, ModelOfferExtractor>();

        services.AddScoped<ExtractOfferFromEmailHandler>();
        services.AddScoped<ReviewOfferHandler>();
        services.AddScoped<OfferQueries>();

        services.AddHostedService<DatabaseInitializer>();

        return services;
    }
}
