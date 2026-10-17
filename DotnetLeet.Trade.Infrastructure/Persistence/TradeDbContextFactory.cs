using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotnetLeet.Trade.Infrastructure.Persistence;

/// <summary>For `dotnet ef migrations add` only. The app gets its connection from Aspire.</summary>
public sealed class TradeDbContextFactory : IDesignTimeDbContextFactory<TradeDbContext>
{
    public TradeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TradeDbContext>()
            .UseNpgsql("Host=localhost;Database=tradedb;Username=postgres;Password=postgres")
            .Options;

        return new TradeDbContext(options);
    }
}
