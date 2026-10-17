using System.Security.Cryptography;
using System.Text;
using DotnetLeet.Trade.Application.Procurement.Ports;
using DotnetLeet.Trade.Domain.Catalog;
using DotnetLeet.Trade.Domain.Procurement;

namespace DotnetLeet.Trade.Application.Procurement.ExtractOffer;

public sealed record ExtractOfferFromEmailCommand(string EmailText, string? SenderAddress);

public sealed record ExtractOfferFromEmailResult(Guid OfferId, bool AlreadyExisted);

/// <summary>
/// Email in, draft offer in the database out. Idempotent: the same email text twice
/// returns the offer that already exists, it does not call the model again.
/// The use case expresses intent and delegates. No prompt text, no retries, no arithmetic here.
/// </summary>
public sealed class ExtractOfferFromEmailHandler(
    IOfferExtractor extractor,
    IOfferRepository offers,
    ISupplierRepository suppliers,
    IProductRepository products,
    IPurchasePriceHistoryRepository priceHistory,
    IAiCallLog aiCalls,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<ExtractOfferFromEmailResult> HandleAsync(ExtractOfferFromEmailCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.EmailText))
            throw new ArgumentException("The email is empty.", nameof(command));

        var hash = SourceHash.Of(command.EmailText);

        var existing = await offers.FindBySourceHashAsync(hash, ct);
        if (existing is not null)
            return new ExtractOfferFromEmailResult(existing.Id, AlreadyExisted: true);

        var extraction = await extractor.ExtractAsync(command.EmailText, ct);
        await aiCalls.WriteAsync(extraction.Call, ct);

        var supplier = await ResolveSupplierAsync(command.SenderAddress, extraction.SupplierName, ct);

        var now = clock.GetUtcNow();
        var offer = SupplierOffer.Draft(
            OfferSource.Email,
            command.EmailText,
            hash,
            command.SenderAddress,
            now,
            supplier?.Id,
            extraction.Call.Id);

        offer.SetHeader(extraction.SupplierReference, extraction.Currency, extraction.ValidUntil, extraction.PaymentTermsDays, extraction.DeliveryDays);

        foreach (var line in extraction.Lines)
            offer.AddLine(line.RawText, line.SupplierCode, line.ManufacturerPartNumber, line.Description, line.Quantity, line.UnitPrice, line.Unit);

        var context = await OfferContexts.BuildAsync(supplier, products, priceHistory, now, ct);
        offer.Evaluate(context);

        await offers.AddAsync(offer, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new ExtractOfferFromEmailResult(offer.Id, AlreadyExisted: false);
    }

    private async Task<Supplier?> ResolveSupplierAsync(string? senderAddress, string? supplierName, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(senderAddress) && senderAddress.Contains('@'))
        {
            var domain = senderAddress[(senderAddress.LastIndexOf('@') + 1)..].Trim().ToLowerInvariant();
            var byDomain = await suppliers.FindByEmailDomainAsync(domain, ct);
            if (byDomain is not null)
                return byDomain;
        }

        if (!string.IsNullOrWhiteSpace(supplierName))
            return await suppliers.FindByNameAsync(supplierName, ct);

        return null;
    }
}

public static class SourceHash
{
    /// <summary>Whitespace and case do not make a different email.</summary>
    public static string Of(string text)
    {
        var normalized = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}

public static class OfferContexts
{
    public static async Task<OfferContext> BuildAsync(
        Supplier? supplier,
        IProductRepository products,
        IPurchasePriceHistoryRepository priceHistory,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var catalog = await products.ListAsync(ct);
        var lastPurchases = await priceHistory.LastPurchasesAsync(ct);

        return new OfferContext(
            supplier,
            new ProductMatcher(catalog),
            catalog.ToDictionary(p => p.Id),
            lastPurchases,
            DateOnly.FromDateTime(now.UtcDateTime));
    }
}
