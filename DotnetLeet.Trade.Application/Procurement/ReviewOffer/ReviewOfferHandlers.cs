using DotnetLeet.Trade.Application.Procurement.ExtractOffer;
using DotnetLeet.Trade.Application.Procurement.Ports;
using DotnetLeet.Trade.Domain.Procurement;

namespace DotnetLeet.Trade.Application.Procurement.ReviewOffer;

public sealed record CorrectOfferLineCommand(Guid OfferId, Guid LineId, decimal? Quantity, decimal? UnitPrice, Guid? ProductId);

public sealed record CorrectOfferHeaderCommand(Guid OfferId, Guid? SupplierId, string? Currency, DateOnly? ValidUntil, int? PaymentTermsDays, int? DeliveryDays);

public sealed record ConfirmOfferReviewCommand(Guid OfferId);

public sealed record RejectOfferCommand(Guid OfferId, string Reason);

/// <summary>
/// A person corrects what the model got wrong. Every correction re-runs the rules,
/// so the issues on screen are always about the current numbers.
/// </summary>
public sealed class ReviewOfferHandler(
    IOfferRepository offers,
    ISupplierRepository suppliers,
    IProductRepository products,
    IPurchasePriceHistoryRepository priceHistory,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task CorrectLineAsync(CorrectOfferLineCommand command, CancellationToken ct = default)
    {
        var offer = await LoadAsync(command.OfferId, ct);
        offer.CorrectLine(command.LineId, command.Quantity, command.UnitPrice, command.ProductId);
        await EvaluateAndSaveAsync(offer, ct);
    }

    public async Task CorrectHeaderAsync(CorrectOfferHeaderCommand command, CancellationToken ct = default)
    {
        var offer = await LoadAsync(command.OfferId, ct);

        if (command.SupplierId is { } supplierId)
        {
            _ = await suppliers.GetAsync(supplierId, ct) ?? throw new InvalidOperationException("No such supplier.");
            offer.AssignSupplier(supplierId);
        }

        offer.CorrectHeader(
            command.Currency ?? offer.Currency,
            command.ValidUntil ?? offer.ValidUntil,
            command.PaymentTermsDays ?? offer.PaymentTermsDays,
            command.DeliveryDays ?? offer.DeliveryDays);

        await EvaluateAndSaveAsync(offer, ct);
    }

    public async Task ConfirmAsync(ConfirmOfferReviewCommand command, CancellationToken ct = default)
    {
        var offer = await LoadAsync(command.OfferId, ct);
        await EvaluateAsync(offer, ct);
        offer.MarkReviewed(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RejectAsync(RejectOfferCommand command, CancellationToken ct = default)
    {
        var offer = await LoadAsync(command.OfferId, ct);
        offer.Reject(command.Reason, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<SupplierOffer> LoadAsync(Guid offerId, CancellationToken ct) =>
        await offers.GetAsync(offerId, ct) ?? throw new InvalidOperationException("No such offer.");

    private async Task EvaluateAndSaveAsync(SupplierOffer offer, CancellationToken ct)
    {
        await EvaluateAsync(offer, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task EvaluateAsync(SupplierOffer offer, CancellationToken ct)
    {
        var supplier = offer.SupplierId is { } id ? await suppliers.GetAsync(id, ct) : null;
        var context = await OfferContexts.BuildAsync(supplier, products, priceHistory, clock.GetUtcNow(), ct);
        offer.Evaluate(context);
    }
}
