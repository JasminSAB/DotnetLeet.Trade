using DotnetLeet.Trade.Application.Procurement.Ports;
using DotnetLeet.Trade.Domain.Catalog;
using DotnetLeet.Trade.Domain.Procurement;
using DotnetLeet.Trade.Infrastructure.Persistence;
using Microsoft.Extensions.AI;

namespace DotnetLeet.Trade.Evals.Fakes;

/// <summary>
/// The seed catalog in memory. Unit tests and evals run against the same products,
/// suppliers and purchase prices the app seeds into PostgreSQL.
/// </summary>
internal sealed class InMemoryTrade : ISupplierRepository, IProductRepository, IPurchasePriceHistoryRepository, IOfferRepository, IAiCallLog, IUnitOfWork
{
    public SeedData Seed { get; }
    public List<SupplierOffer> Offers { get; } = [];
    public List<AiCall> AiCalls { get; } = [];
    public int SaveCount { get; private set; }

    public InMemoryTrade(DateOnly today)
    {
        Seed = SeedData.Create(today);
    }

    public Task<Supplier?> FindByEmailDomainAsync(string emailDomain, CancellationToken ct = default) =>
        Task.FromResult(Seed.Suppliers.FirstOrDefault(s => s.EmailDomain == emailDomain.ToLowerInvariant()));

    public Task<Supplier?> FindByNameAsync(string name, CancellationToken ct = default) =>
        Task.FromResult(Seed.Suppliers.FirstOrDefault(s => string.Equals(s.Name, name.Trim(), StringComparison.OrdinalIgnoreCase) || string.Equals(s.Code, name.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<Supplier?> GetAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Seed.Suppliers.FirstOrDefault(s => s.Id == id));

    Task<IReadOnlyList<Supplier>> ISupplierRepository.ListAsync(CancellationToken ct) =>
        Task.FromResult(Seed.Suppliers);

    Task<IReadOnlyList<Product>> IProductRepository.ListAsync(CancellationToken ct) =>
        Task.FromResult(Seed.Products);

    public Task<IReadOnlyList<Product>> SearchAsync(string term, int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Product>>(Seed.Products.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || p.Sku.Contains(term, StringComparison.OrdinalIgnoreCase)).Take(take).ToList());

    public Task<IReadOnlyDictionary<Guid, LastPurchase>> LastPurchasesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, LastPurchase>>(Seed.PurchasePrices
            .GroupBy(p => p.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.PurchasedAt).First().ToLastPurchase()));

    public Task<SupplierOffer?> FindBySourceHashAsync(string sourceHash, CancellationToken ct = default) =>
        Task.FromResult(Offers.FirstOrDefault(o => o.SourceHash == sourceHash));

    Task<SupplierOffer?> IOfferRepository.GetAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Offers.FirstOrDefault(o => o.Id == id));

    public Task<IReadOnlyList<SupplierOffer>> ListAsync(int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SupplierOffer>>(Offers.Take(take).ToList());

    public Task AddAsync(SupplierOffer offer, CancellationToken ct = default)
    {
        Offers.Add(offer);
        return Task.CompletedTask;
    }

    public Task WriteAsync(AiCall call, CancellationToken ct = default)
    {
        AiCalls.Add(call);
        return Task.CompletedTask;
    }

    Task<IReadOnlyList<AiCall>> IAiCallLog.ListAsync(int take, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AiCall>>(AiCalls.Take(take).ToList());

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

/// <summary>An IChatClient that answers with a fixed string. The seam for tests about our code, not the model.</summary>
internal sealed class CannedChatClient(string answer) : IChatClient
{
    public int Calls { get; private set; }
    public IList<ChatMessage> LastMessages { get; private set; } = [];
    public ChatOptions? LastOptions { get; private set; }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Calls++;
        LastMessages = messages.ToList();
        LastOptions = options;
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer))
        {
            ModelId = "canned",
            Usage = new UsageDetails { InputTokenCount = 100, OutputTokenCount = 50 }
        });
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
