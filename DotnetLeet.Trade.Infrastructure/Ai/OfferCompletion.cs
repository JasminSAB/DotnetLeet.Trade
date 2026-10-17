using System.ComponentModel;
using System.Globalization;
using DotnetLeet.Trade.Application.Procurement.Ports;

namespace DotnetLeet.Trade.Infrastructure.Ai;

/// <summary>
/// The shape the model fills in. A DTO: it is the JSON schema the model sees and it
/// lives next to the adapter that reads it. Nothing above Infrastructure sees this type.
/// </summary>
internal sealed class OfferCompletion
{
    [Description("Supplier name exactly as written in the email, or null.")]
    public string? SupplierName { get; set; }

    [Description("The supplier's own quote or offer number, or null.")]
    public string? SupplierReference { get; set; }

    [Description("Three-letter ISO currency code of the prices, only if the email states it. Otherwise null.")]
    public string? Currency { get; set; }

    [Description("Date until which the offer is valid, as yyyy-MM-dd, only if a date is written. Otherwise null.")]
    public string? ValidUntil { get; set; }

    [Description("Payment terms in days, only if stated as days. Otherwise null.")]
    public int? PaymentTermsDays { get; set; }

    [Description("Delivery time in days, only if stated. In stock means 0. A range means the higher number. Otherwise null.")]
    public int? DeliveryDays { get; set; }

    [Description("One entry per product line in the email.")]
    public List<LineCompletion> Lines { get; set; } = [];

    [Description("One short sentence per thing that could not be determined and why.")]
    public List<string> Unknowns { get; set; } = [];

    public (OfferExtraction Extraction, List<string> Problems) ToExtraction(AiCall call)
    {
        var problems = new List<string>();
        DateOnly? validUntil = null;

        if (!string.IsNullOrWhiteSpace(ValidUntil))
        {
            if (DateOnly.TryParseExact(ValidUntil.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                validUntil = parsed;
            else
                problems.Add($"Validity date '{ValidUntil}' is not a date.");
        }

        var currency = string.IsNullOrWhiteSpace(Currency) ? null : Currency.Trim().ToUpperInvariant();
        if (currency is not null && currency.Length != 3)
        {
            problems.Add($"Currency '{Currency}' is not an ISO code.");
            currency = null;
        }

        var lines = Lines
            .Where(l => !string.IsNullOrWhiteSpace(l.RawText) || !string.IsNullOrWhiteSpace(l.Description))
            .Select(l => new ExtractedLine(
                string.IsNullOrWhiteSpace(l.RawText) ? l.Description!.Trim() : l.RawText.Trim(),
                l.SupplierCode,
                l.ManufacturerPartNumber,
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.Unit))
            .ToList();

        var unknowns = Unknowns.Where(u => !string.IsNullOrWhiteSpace(u)).Concat(problems).ToList();

        return (new OfferExtraction(
            SupplierName,
            SupplierReference,
            currency,
            validUntil,
            PaymentTermsDays,
            DeliveryDays,
            lines,
            unknowns,
            call), problems);
    }
}

internal sealed class LineCompletion
{
    [Description("The line verbatim as written in the email.")]
    public string RawText { get; set; } = "";

    [Description("The supplier's article number for this line, or null.")]
    public string? SupplierCode { get; set; }

    [Description("The manufacturer part number for this line, or null.")]
    public string? ManufacturerPartNumber { get; set; }

    [Description("The product name as written.")]
    public string? Description { get; set; }

    [Description("Offered quantity as a number, or null if not written.")]
    public decimal? Quantity { get; set; }

    [Description("Price per unit as a number without currency, or null if not written. Never computed.")]
    public decimal? UnitPrice { get; set; }

    [Description("The unit word as written (pcs, pack, box), or null.")]
    public string? Unit { get; set; }
}
