namespace DotnetLeet.Trade.Domain.Inventory;

/// <summary>
/// How many we have. Only a confirmed goods receipt changes OnHand. An offer never does.
/// </summary>
public sealed class StockLevel
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal OnHand { get; private set; }
    public decimal Reserved { get; private set; }
    public decimal Available => OnHand - Reserved;

    private StockLevel() { }

    public static StockLevel Start(Guid productId, decimal onHand)
    {
        if (onHand < 0) throw new ArgumentException("Stock cannot start below zero.", nameof(onHand));
        return new StockLevel { Id = Guid.NewGuid(), ProductId = productId, OnHand = onHand };
    }

    public void Receive(decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentException("A receipt is above zero.", nameof(quantity));
        OnHand += quantity;
    }
}
