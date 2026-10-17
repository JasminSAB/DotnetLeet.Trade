using DotnetLeet.Trade.Domain.Catalog;

namespace DotnetLeet.Trade.Domain.Procurement;

/// <summary>
/// One line of a supplier offer. Nullable numbers are the point: the model read what it
/// could, and what it could not read stays null and becomes an issue. Nothing here guesses.
/// </summary>
public sealed class OfferLine
{
    public Guid Id { get; private set; }
    public Guid OfferId { get; private set; }
    public int LineNo { get; private set; }

    /// <summary>The line as the supplier wrote it. Kept so a reviewer can check the extraction.</summary>
    public string RawText { get; private set; } = "";
    public string? SupplierCode { get; private set; }
    public string? ManufacturerPartNumber { get; private set; }
    public string? Description { get; private set; }
    public decimal? Quantity { get; private set; }
    public decimal? UnitPrice { get; private set; }
    public string? Unit { get; private set; }

    public Guid? ProductId { get; private set; }
    public MatchMethod MatchMethod { get; private set; }
    public bool CorrectedByReviewer { get; private set; }

    /// <summary>Quantity times unit price, computed by us, null while either is unknown.</summary>
    public decimal? LineTotal { get; private set; }

    private readonly List<OfferIssue> _issues = [];
    public IReadOnlyList<OfferIssue> Issues => _issues;

    private OfferLine() { }

    internal static OfferLine Create(Guid offerId, int lineNo, string rawText, string? supplierCode, string? mpn, string? description, decimal? quantity, decimal? unitPrice, string? unit)
    {
        var line = new OfferLine
        {
            Id = Guid.NewGuid(),
            OfferId = offerId,
            LineNo = lineNo,
            RawText = rawText.Trim(),
            SupplierCode = Clean(supplierCode),
            ManufacturerPartNumber = Clean(mpn),
            Description = Clean(description),
            Quantity = quantity,
            UnitPrice = unitPrice,
            Unit = Clean(unit)
        };
        line.Recalculate();
        return line;
    }

    internal void Match(ProductMatch match)
    {
        if (CorrectedByReviewer && ProductId is not null)
            return;

        ProductId = match.ProductId;
        MatchMethod = match.Method;
    }

    internal void Correct(decimal? quantity, decimal? unitPrice, Guid? productId, string? supplierCode, string? mpn)
    {
        if (quantity is not null) Quantity = quantity;
        if (unitPrice is not null) UnitPrice = unitPrice;
        if (supplierCode is not null) SupplierCode = Clean(supplierCode);
        if (mpn is not null) ManufacturerPartNumber = Clean(mpn);
        if (productId is not null)
        {
            ProductId = productId;
            MatchMethod = MatchMethod.None;
        }
        CorrectedByReviewer = true;
        Recalculate();
    }

    internal void ClearIssues() => _issues.Clear();
    internal void AddIssue(OfferIssue issue) => _issues.Add(issue);

    private void Recalculate()
    {
        LineTotal = Quantity is { } q && UnitPrice is { } p ? decimal.Round(q * p, 2, MidpointRounding.ToEven) : null;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
