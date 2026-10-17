using DotnetLeet.Trade.Application.Procurement.ExtractOffer;
using DotnetLeet.Trade.Application.Procurement.Ports;
using DotnetLeet.Trade.Application.Procurement.Queries;
using DotnetLeet.Trade.Application.Procurement.ReviewOffer;
using DotnetLeet.Trade.Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace DotnetLeet.Trade.Evals.Fakes;

/// <summary>
/// The application wired like the API wires it, with the database swapped for memory
/// and the model swapped for whatever the test hands in.
/// </summary>
internal sealed class TradeHost
{
    public static readonly DateOnly Today = new(2026, 10, 8);

    public InMemoryTrade Store { get; }
    public ServiceProvider Services { get; }

    public ExtractOfferFromEmailHandler Extract => Services.GetRequiredService<ExtractOfferFromEmailHandler>();
    public ReviewOfferHandler Review => Services.GetRequiredService<ReviewOfferHandler>();
    public OfferQueries Queries => Services.GetRequiredService<OfferQueries>();

    public TradeHost(IChatClient chatClient)
    {
        Store = new InMemoryTrade(Today);
        var clock = new FakeTimeProvider(new DateTimeOffset(Today, new TimeOnly(9, 0), TimeSpan.Zero));

        var services = new ServiceCollection();
        services.AddTrade(new ConfigurationBuilder().Build());

        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(chatClient);
        services.AddSingleton<ISupplierRepository>(Store);
        services.AddSingleton<IProductRepository>(Store);
        services.AddSingleton<IPurchasePriceHistoryRepository>(Store);
        services.AddSingleton<IOfferRepository>(Store);
        services.AddSingleton<IAiCallLog>(Store);
        services.AddSingleton<IUnitOfWork>(Store);

        // the hosted database initializer is not wanted here
        var initializer = services.Single(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService));
        services.Remove(initializer);

        Services = services.BuildServiceProvider();
    }
}
