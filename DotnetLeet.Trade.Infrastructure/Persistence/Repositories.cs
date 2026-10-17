using DotnetLeet.Trade.Application.Procurement.Ports;
using DotnetLeet.Trade.Domain.Catalog;
using DotnetLeet.Trade.Domain.Procurement;
using Microsoft.EntityFrameworkCore;

namespace DotnetLeet.Trade.Infrastructure.Persistence;

internal sealed class SupplierRepository(TradeDbContext db) : ISupplierRepository
{
    public Task<Supplier?> FindByEmailDomainAsync(string emailDomain, CancellationToken ct = default) =>
        db.Suppliers.FirstOrDefaultAsync(s => s.Active && s.EmailDomain == emailDomain.ToLower(), ct);

    public Task<Supplier?> FindByNameAsync(string name, CancellationToken ct = default)
    {
        var needle = name.Trim().ToLower();
        return db.Suppliers.FirstOrDefaultAsync(s => s.Active && (s.Name.ToLower() == needle || s.Code.ToLower() == needle), ct);
    }

    public Task<Supplier?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Supplier>> ListAsync(CancellationToken ct = default) =>
        await db.Suppliers.Where(s => s.Active).OrderBy(s => s.Name).ToListAsync(ct);
}

internal sealed class ProductRepository(TradeDbContext db) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct = default) =>
        await db.Products.Include(p => p.Aliases).Where(p => p.Active).AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> SearchAsync(string term, int take, CancellationToken ct = default)
    {
        var needle = term.Trim().ToLower();
        if (needle.Length == 0)
            return [];

        return await db.Products
            .Where(p => p.Active && (p.Sku.ToLower().Contains(needle) || p.Name.ToLower().Contains(needle) || p.ManufacturerPartNumber.ToLower().Contains(needle)))
            .OrderBy(p => p.Name)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}

internal sealed class PurchasePriceHistoryRepository(TradeDbContext db) : IPurchasePriceHistoryRepository
{
    public async Task<IReadOnlyDictionary<Guid, LastPurchase>> LastPurchasesAsync(CancellationToken ct = default)
    {
        var rows = await db.PurchasePrices.AsNoTracking().ToListAsync(ct);

        return rows
            .GroupBy(p => p.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.PurchasedAt).First().ToLastPurchase());
    }
}

internal sealed class OfferRepository(TradeDbContext db) : IOfferRepository
{
    public Task<SupplierOffer?> FindBySourceHashAsync(string sourceHash, CancellationToken ct = default) =>
        db.Offers.Include(o => o.Lines).FirstOrDefaultAsync(o => o.SourceHash == sourceHash, ct);

    public Task<SupplierOffer?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Offers.Include(o => o.Lines.OrderBy(l => l.LineNo)).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<SupplierOffer>> ListAsync(int take, CancellationToken ct = default) =>
        await db.Offers.Include(o => o.Lines).OrderByDescending(o => o.ReceivedAt).Take(take).AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(SupplierOffer offer, CancellationToken ct = default) =>
        await db.Offers.AddAsync(offer, ct);
}

internal sealed class EfAiCallLog(TradeDbContext db) : IAiCallLog
{
    public async Task WriteAsync(AiCall call, CancellationToken ct = default) =>
        await db.AiCalls.AddAsync(call, ct);

    public async Task<IReadOnlyList<AiCall>> ListAsync(int take, CancellationToken ct = default) =>
        await db.AiCalls.OrderByDescending(c => c.At).Take(take).AsNoTracking().ToListAsync(ct);
}

internal sealed class EfUnitOfWork(TradeDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
