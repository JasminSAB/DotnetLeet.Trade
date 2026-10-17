using DotnetLeet.Trade.Domain.Catalog;

namespace DotnetLeet.Trade.Domain.Procurement;

public enum OfferStatus
{
    /// <summary>Extracted, not yet looked at by a person.</summary>
    Draft,
    /// <summary>A person went through every line and confirmed it.</summary>
    Reviewed,
    Rejected
}

public enum OfferSource
{
    Email,
    Manual
}

/// <summary>
/// What a supplier offered us. The model drafts it from an email, a person reviews it,
/// the rules decide what is wrong with it, and the arithmetic is ours. An offer never
/// changes stock and never becomes a purchase order on its own.
/// </summary>
public sealed class SupplierOffer
{
    public Guid Id { get; private set; }
    public Guid? SupplierId { get; private set; }
    public OfferSource Source { get; private set; }
    /// <summary>Hash of the normalized source text. The same email twice is the same offer.</summary>
    public string SourceHash { get; private set; } = "";
    public string SourceText { get; private set; } = "";
    public string? SenderAddress { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }

    public string? SupplierReference { get; private set; }
    public string? Currency { get; private set; }
    public DateOnly? ValidUntil { get; private set; }
    public int? PaymentTermsDays { get; private set; }
    public int? DeliveryDays { get; private set; }

    public OfferStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>The AI call that produced the draft, for the audit trail. Null for a manual offer.</summary>
    public Guid? ExtractionCallId { get; private set; }

    private readonly List<OfferLine> _lines = [];
    public IReadOnlyList<OfferLine> Lines => _lines;

    private readonly List<OfferIssue> _issues = [];
    /// <summary>Offer-level issues. Line issues live on the line.</summary>
    public IReadOnlyList<OfferIssue> Issues => _issues;

    public decimal Subtotal { get; private set; }
    public int UnpricedLines { get; private set; }

    private SupplierOffer() { }

    public static SupplierOffer Draft(
        OfferSource source,
        string sourceText,
        string sourceHash,
        string? senderAddress,
        DateTimeOffset receivedAt,
        Guid? supplierId,
        Guid? extractionCallId)
    {
        if (string.IsNullOrWhiteSpace(sourceText)) throw new ArgumentException("An offer needs its source text.", nameof(sourceText));
        if (string.IsNullOrWhiteSpace(sourceHash)) throw new ArgumentException("An offer needs a source hash.", nameof(sourceHash));

        return new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Source = source,
            SourceText = sourceText,
            SourceHash = sourceHash,
            SenderAddress = senderAddress,
            ReceivedAt = receivedAt,
            SupplierId = supplierId,
            ExtractionCallId = extractionCallId,
            Status = OfferStatus.Draft
        };
    }

    public void SetHeader(string? supplierReference, string? currency, DateOnly? validUntil, int? paymentTermsDays, int? deliveryDays)
    {
        SupplierReference = Clean(supplierReference);
        Currency = Clean(currency)?.ToUpperInvariant();
        ValidUntil = validUntil;
        PaymentTermsDays = paymentTermsDays;
        DeliveryDays = deliveryDays;
    }

    public OfferLine AddLine(string rawText, string? supplierCode, string? mpn, string? description, decimal? quantity, decimal? unitPrice, string? unit)
    {
        var line = OfferLine.Create(Id, _lines.Count + 1, rawText, supplierCode, mpn, description, quantity, unitPrice, unit);
        _lines.Add(line);
        return line;
    }

    public void AssignSupplier(Guid supplierId)
    {
        EnsureDraft();
        SupplierId = supplierId;
    }

    public void CorrectLine(Guid lineId, decimal? quantity, decimal? unitPrice, Guid? productId, string? supplierCode = null, string? mpn = null)
    {
        EnsureDraft();
        var line = _lines.SingleOrDefault(l => l.Id == lineId) ?? throw new InvalidOperationException("No such line.");
        line.Correct(quantity, unitPrice, productId, supplierCode, mpn);
    }

    public void CorrectHeader(string? currency, DateOnly? validUntil, int? paymentTermsDays, int? deliveryDays)
    {
        EnsureDraft();
        SetHeader(SupplierReference, currency, validUntil, paymentTermsDays, deliveryDays);
    }

    /// <summary>
    /// Runs the rules and the arithmetic. Called after extraction and after every correction.
    /// </summary>
    public void Evaluate(OfferContext context)
    {
        _issues.Clear();
        foreach (var line in _lines)
            line.ClearIssues();

        OfferRules.Apply(this, context);

        Subtotal = _lines.Where(l => l.LineTotal is not null).Sum(l => l.LineTotal!.Value);
        UnpricedLines = _lines.Count(l => l.LineTotal is null);
    }

    public bool HasBlockingIssues =>
        _issues.Any(i => i.Severity == IssueSeverity.Blocking) || _lines.Any(l => l.Issues.Any(i => i.Severity == IssueSeverity.Blocking));

    public void MarkReviewed(DateTimeOffset at)
    {
        EnsureDraft();
        if (HasBlockingIssues)
            throw new InvalidOperationException("An offer with blocking issues cannot be marked as reviewed.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("An offer without lines cannot be marked as reviewed.");

        Status = OfferStatus.Reviewed;
        ReviewedAt = at;
    }

    public void Reject(string reason, DateTimeOffset at)
    {
        EnsureDraft();
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A rejection needs a reason.", nameof(reason));

        Status = OfferStatus.Rejected;
        RejectionReason = reason.Trim();
        ReviewedAt = at;
    }

    internal void AddIssue(OfferIssue issue) => _issues.Add(issue);

    private void EnsureDraft()
    {
        if (Status != OfferStatus.Draft)
            throw new InvalidOperationException($"The offer is {Status}, it can no longer change.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>What the rules need to know that is not on the offer itself.</summary>
public sealed record OfferContext(
    Supplier? Supplier,
    ProductMatcher Matcher,
    IReadOnlyDictionary<Guid, Product> Products,
    IReadOnlyDictionary<Guid, LastPurchase> LastPurchases,
    DateOnly Today);

public sealed record LastPurchase(Guid ProductId, decimal UnitPrice, string Currency, DateOnly PurchasedAt);
