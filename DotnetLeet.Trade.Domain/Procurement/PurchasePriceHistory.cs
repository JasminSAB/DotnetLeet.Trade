namespace DotnetLeet.Trade.Domain.Procurement;

/// <summary>
/// What we paid last time. The yardstick for a new offer's prices.
/// </summary>
public sealed class PurchasePriceHistory
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid SupplierId { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; } = "";
    public DateOnly PurchasedAt { get; private set; }

    private PurchasePriceHistory() { }

    public static PurchasePriceHistory Record(Guid productId, Guid supplierId, decimal unitPrice, string currency, DateOnly purchasedAt)
    {
        if (unitPrice <= 0) throw new ArgumentException("A purchase price is above zero.", nameof(unitPrice));

        return new PurchasePriceHistory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            SupplierId = supplierId,
            UnitPrice = unitPrice,
            Currency = currency.ToUpperInvariant(),
            PurchasedAt = purchasedAt
        };
    }

    public LastPurchase ToLastPurchase() => new(ProductId, UnitPrice, Currency, PurchasedAt);
}
