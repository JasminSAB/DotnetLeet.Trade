using DotnetLeet.Trade.Domain.Catalog;
using DotnetLeet.Trade.Domain.Procurement;

namespace DotnetLeet.Trade.Application.Procurement.Ports;

public interface ISupplierRepository
{
    Task<Supplier?> FindByEmailDomainAsync(string emailDomain, CancellationToken ct = default);
    Task<Supplier?> FindByNameAsync(string name, CancellationToken ct = default);
    Task<Supplier?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Supplier>> ListAsync(CancellationToken ct = default);
}

public interface IProductRepository
{
    /// <summary>The whole catalog with aliases. Small enough to hold in memory for matching.</summary>
    Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> SearchAsync(string term, int take, CancellationToken ct = default);
}

public interface IPurchasePriceHistoryRepository
{
    Task<IReadOnlyDictionary<Guid, LastPurchase>> LastPurchasesAsync(CancellationToken ct = default);
}

public interface IOfferRepository
{
    Task<SupplierOffer?> FindBySourceHashAsync(string sourceHash, CancellationToken ct = default);
    Task<SupplierOffer?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SupplierOffer>> ListAsync(int take, CancellationToken ct = default);
    Task AddAsync(SupplierOffer offer, CancellationToken ct = default);
}

public interface IAiCallLog
{
    Task WriteAsync(AiCall call, CancellationToken ct = default);
    Task<IReadOnlyList<AiCall>> ListAsync(int take, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}
