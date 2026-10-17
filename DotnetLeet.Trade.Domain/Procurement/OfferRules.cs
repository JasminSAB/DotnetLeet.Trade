using System.Globalization;
using DotnetLeet.Trade.Domain.Catalog;

namespace DotnetLeet.Trade.Domain.Procurement;

/// <summary>
/// What is wrong with an offer. These rules do not know an email or a model exists.
/// They look at the offer, the catalog and the purchase history, and they say so.
/// </summary>
public static class OfferRules
{
    /// <summary>A unit price this far above the last purchase price is flagged.</summary>
    public const decimal PriceDeviationThreshold = 0.15m;

    /// <summary>
    /// A unit price this many times the last purchase price is not a price increase, it is a
    /// wrong number: a line total read as a unit price, a misplaced decimal point. It blocks.
    /// </summary>
    public const decimal GrossDeviationFactor = 3m;

    public static void Apply(SupplierOffer offer, OfferContext context)
    {
        if (offer.Lines.Count == 0)
            offer.AddIssue(OfferIssue.Blocking(IssueCode.NoLines, "The offer has no lines."));

        if (context.Supplier is null)
            offer.AddIssue(OfferIssue.Blocking(IssueCode.UnknownSupplier, "The sender is not a known supplier. Pick one."));

        if (offer.Currency is null)
            offer.AddIssue(OfferIssue.Blocking(IssueCode.MissingCurrency, "The offer does not say which currency the prices are in."));
        else if (context.Supplier is not null && !string.Equals(offer.Currency, context.Supplier.Currency, StringComparison.OrdinalIgnoreCase))
            offer.AddIssue(OfferIssue.Warning(IssueCode.CurrencyMismatch, $"The offer is in {offer.Currency}, this supplier usually quotes in {context.Supplier.Currency}."));

        if (offer.ValidUntil is null)
            offer.AddIssue(OfferIssue.Warning(IssueCode.MissingValidity, "The offer does not say how long it is valid."));
        else if (offer.ValidUntil < context.Today)
            offer.AddIssue(OfferIssue.Blocking(IssueCode.OfferExpired, $"The offer expired on {offer.ValidUntil:yyyy-MM-dd}."));

        var seenProducts = new HashSet<Guid>();

        foreach (var line in offer.Lines)
        {
            if (line.ProductId is null || !line.CorrectedByReviewer)
                line.Match(context.Matcher.Match(offer.SupplierId, line.SupplierCode, line.ManufacturerPartNumber, rawText: line.RawText));

            if (line.ProductId is null)
                line.AddIssue(OfferIssue.Blocking(IssueCode.UnmatchedProduct, "No product in the catalog has this code. Pick one or reject the line."));

            if (line.Quantity is null)
                line.AddIssue(OfferIssue.Blocking(IssueCode.MissingQuantity, "The quantity could not be read from the offer."));
            else if (line.Quantity <= 0)
                line.AddIssue(OfferIssue.Blocking(IssueCode.MissingQuantity, "The quantity must be above zero."));

            if (line.UnitPrice is null)
                line.AddIssue(OfferIssue.Blocking(IssueCode.MissingUnitPrice, "The unit price could not be read from the offer."));
            else if (line.UnitPrice <= 0)
                line.AddIssue(OfferIssue.Blocking(IssueCode.MissingUnitPrice, "The unit price must be above zero."));

            if (line.ProductId is { } productId)
            {
                if (!seenProducts.Add(productId))
                    line.AddIssue(OfferIssue.Warning(IssueCode.DuplicateLine, "The same product appears on another line."));

                if (context.Products.TryGetValue(productId, out var product)
                    && line.Unit is not null
                    && !UnitMatches(product.Unit, line.Unit))
                    line.AddIssue(OfferIssue.Warning(IssueCode.UnitMismatch, $"The supplier quotes per {line.Unit}, our catalog counts this product per {product.Unit}."));

                if (line.UnitPrice is { } price
                    && offer.Currency is not null
                    && context.LastPurchases.TryGetValue(productId, out var last)
                    && string.Equals(last.Currency, offer.Currency, StringComparison.OrdinalIgnoreCase)
                    && last.UnitPrice > 0)
                {
                    var lastPrice = last.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture);

                    if (price >= last.UnitPrice * GrossDeviationFactor)
                        line.AddIssue(OfferIssue.Blocking(IssueCode.PriceDeviation, $"{decimal.Round(price / last.UnitPrice, 1).ToString("0.#", CultureInfo.InvariantCulture)} times the last purchase price of {lastPrice} {last.Currency}. A line total or a misplaced decimal point, check the email."));
                    else if (price > last.UnitPrice * (1 + PriceDeviationThreshold))
                    {
                        var percent = decimal.Round((price / last.UnitPrice - 1) * 100, 0).ToString("0", CultureInfo.InvariantCulture);
                        line.AddIssue(OfferIssue.Warning(IssueCode.PriceDeviation, $"{percent}% above the last purchase price of {lastPrice} {last.Currency} on {last.PurchasedAt:yyyy-MM-dd}."));
                    }
                }
            }
        }
    }

    private static bool UnitMatches(Unit catalogUnit, string offerUnit)
    {
        var u = offerUnit.Trim().ToLowerInvariant();
        return catalogUnit switch
        {
            Unit.Piece => u is "pc" or "pcs" or "piece" or "pieces" or "ea" or "each" or "unit" or "units" or "kom",
            Unit.Pack => u is "pack" or "packs" or "pk" or "pak" or "pakovanje" or "box" or "boxes",
            _ => true
        };
    }
}
