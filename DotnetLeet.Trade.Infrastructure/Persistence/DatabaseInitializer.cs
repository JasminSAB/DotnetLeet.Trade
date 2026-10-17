using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotnetLeet.Trade.Infrastructure.Persistence;

/// <summary>
/// Applies migrations and seeds an empty database at startup. Development convenience;
/// a production deployment runs migrations as a separate step.
/// </summary>
public sealed class DatabaseInitializer(IServiceProvider services, TimeProvider clock, ILogger<DatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TradeDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Products.AnyAsync(cancellationToken))
            return;

        var seed = SeedData.Create(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));

        db.Suppliers.AddRange(seed.Suppliers);
        db.Products.AddRange(seed.Products);
        db.PurchasePrices.AddRange(seed.PurchasePrices);
        db.StockLevels.AddRange(seed.StockLevels);
        db.PurchaseRequests.AddRange(seed.PurchaseRequests);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Products} products, {Suppliers} suppliers, {Prices} purchase prices.",
            seed.Products.Count, seed.Suppliers.Count, seed.PurchasePrices.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
