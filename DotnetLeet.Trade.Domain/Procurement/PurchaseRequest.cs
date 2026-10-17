namespace DotnetLeet.Trade.Domain.Procurement;

public enum PurchaseRequestStatus
{
    Open,
    Closed
}

/// <summary>
/// What we want to buy and why. Offers are compared against requests, and a purchase
/// order needs an approved request behind it. Level 1 only stores them.
/// </summary>
public sealed class PurchaseRequest
{
    public Guid Id { get; private set; }
    public string Number { get; private set; } = "";
    public string RequestedBy { get; private set; } = "";
    public string Reason { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public PurchaseRequestStatus Status { get; private set; }

    private readonly List<PurchaseRequestLine> _lines = [];
    public IReadOnlyList<PurchaseRequestLine> Lines => _lines;

    private PurchaseRequest() { }

    public static PurchaseRequest Open(string number, string requestedBy, string reason, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("A request needs a number.", nameof(number));

        return new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            Number = number.Trim(),
            RequestedBy = requestedBy.Trim(),
            Reason = reason.Trim(),
            CreatedAt = createdAt,
            Status = PurchaseRequestStatus.Open
        };
    }

    public PurchaseRequest WithLine(Guid productId, decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be above zero.", nameof(quantity));
        _lines.Add(PurchaseRequestLine.Create(Id, productId, quantity));
        return this;
    }

    public void Close()
    {
        Status = PurchaseRequestStatus.Closed;
    }
}

public sealed class PurchaseRequestLine
{
    public Guid Id { get; private set; }
    public Guid PurchaseRequestId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }

    private PurchaseRequestLine() { }

    internal static PurchaseRequestLine Create(Guid requestId, Guid productId, decimal quantity) =>
        new() { Id = Guid.NewGuid(), PurchaseRequestId = requestId, ProductId = productId, Quantity = quantity };
}
