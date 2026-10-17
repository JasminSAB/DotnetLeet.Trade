using DotnetLeet.Trade.Application.Procurement.Ports;
using DotnetLeet.Trade.Domain.Catalog;
using DotnetLeet.Trade.Domain.Procurement;

namespace DotnetLeet.Trade.Application.Procurement.Queries;

public sealed record OfferSummary(
    Guid Id,
    string? SupplierName,
    string? SupplierReference,
    string? Currency,
    decimal Subtotal,
    int Lines,
    int BlockingIssues,
    OfferStatus Status,
    DateTimeOffset ReceivedAt);

public sealed record OfferDetail(
    Guid Id,
    Guid? SupplierId,
    string? SupplierName,
    string? SenderAddress,
    string? SupplierReference,
    string? Currency,
    DateOnly? ValidUntil,
    int? PaymentTermsDays,
    int? DeliveryDays,
    OfferStatus Status,
    string? RejectionReason,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? ReviewedAt,
    decimal Subtotal,
    int UnpricedLines,
    ApprovalLevel ApprovalIfOrdered,
    IReadOnlyList<OfferIssue> Issues,
    IReadOnlyList<OfferLineDetail> Lines,
    string SourceText,
    Guid? ExtractionCallId);

public sealed record OfferLineDetail(
    Guid Id,
    int LineNo,
    string RawText,
    string? SupplierCode,
    string? ManufacturerPartNumber,
    string? Description,
    decimal? Quantity,
    decimal? UnitPrice,
    string? Unit,
    decimal? LineTotal,
    Guid? ProductId,
    string? ProductSku,
    string? ProductName,
    MatchMethod MatchMethod,
    bool CorrectedByReviewer,
    IReadOnlyList<OfferIssue> Issues);

public sealed record ProductSummary(Guid Id, string Sku, string Name, string Manufacturer, string ManufacturerPartNumber);

public sealed record SupplierSummary(Guid Id, string Code, string Name, string Currency);

public sealed class OfferQueries(IOfferRepository offers, ISupplierRepository suppliers, IProductRepository products, IAiCallLog aiCalls)
{
    public async Task<IReadOnlyList<OfferSummary>> ListAsync(int take, CancellationToken ct = default)
    {
        var list = await offers.ListAsync(take, ct);
        var supplierNames = (await suppliers.ListAsync(ct)).ToDictionary(s => s.Id, s => s.Name);

        return list.Select(o => new OfferSummary(
            o.Id,
            o.SupplierId is { } sid && supplierNames.TryGetValue(sid, out var name) ? name : null,
            o.SupplierReference,
            o.Currency,
            o.Subtotal,
            o.Lines.Count,
            o.Issues.Count(i => i.Severity == IssueSeverity.Blocking) + o.Lines.Sum(l => l.Issues.Count(i => i.Severity == IssueSeverity.Blocking)),
            o.Status,
            o.ReceivedAt)).ToList();
    }

    public async Task<OfferDetail?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var offer = await offers.GetAsync(id, ct);
        if (offer is null)
            return null;

        var supplier = offer.SupplierId is { } sid ? await suppliers.GetAsync(sid, ct) : null;
        var catalog = (await products.ListAsync(ct)).ToDictionary(p => p.Id);

        var lines = offer.Lines.Select(l =>
        {
            var product = l.ProductId is { } pid && catalog.TryGetValue(pid, out var p) ? p : null;
            return new OfferLineDetail(
                l.Id, l.LineNo, l.RawText, l.SupplierCode, l.ManufacturerPartNumber, l.Description,
                l.Quantity, l.UnitPrice, l.Unit, l.LineTotal,
                l.ProductId, product?.Sku, product?.Name, l.MatchMethod, l.CorrectedByReviewer, l.Issues);
        }).ToList();

        var approval = string.Equals(offer.Currency, ApprovalRules.BaseCurrency, StringComparison.OrdinalIgnoreCase)
            ? ApprovalRules.RequiredFor(offer.Subtotal)
            : ApprovalLevel.None;

        return new OfferDetail(
            offer.Id, offer.SupplierId, supplier?.Name, offer.SenderAddress, offer.SupplierReference, offer.Currency,
            offer.ValidUntil, offer.PaymentTermsDays, offer.DeliveryDays, offer.Status, offer.RejectionReason,
            offer.ReceivedAt, offer.ReviewedAt, offer.Subtotal, offer.UnpricedLines, approval,
            offer.Issues, lines, offer.SourceText, offer.ExtractionCallId);
    }

    public async Task<IReadOnlyList<ProductSummary>> SearchProductsAsync(string term, int take, CancellationToken ct = default)
    {
        var found = await products.SearchAsync(term, take, ct);
        return found.Select(p => new ProductSummary(p.Id, p.Sku, p.Name, p.Manufacturer, p.ManufacturerPartNumber)).ToList();
    }

    public async Task<IReadOnlyList<SupplierSummary>> ListSuppliersAsync(CancellationToken ct = default)
    {
        var list = await suppliers.ListAsync(ct);
        return list.Select(s => new SupplierSummary(s.Id, s.Code, s.Name, s.Currency)).ToList();
    }

    public Task<IReadOnlyList<AiCall>> ListAiCallsAsync(int take, CancellationToken ct = default) => aiCalls.ListAsync(take, ct);
}
