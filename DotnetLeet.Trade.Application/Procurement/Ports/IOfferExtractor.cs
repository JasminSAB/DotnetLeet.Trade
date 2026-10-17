namespace DotnetLeet.Trade.Application.Procurement.Ports;

/// <summary>
/// The port. Email text in, a draft offer out. It does not say "AI" and a person
/// with a form could implement it. What comes back is a draft: nullable numbers,
/// strings as the supplier wrote them, and a list of what could not be found.
/// </summary>
public interface IOfferExtractor
{
    Task<OfferExtraction> ExtractAsync(string emailText, CancellationToken ct = default);
}

public sealed record OfferExtraction(
    string? SupplierName,
    string? SupplierReference,
    string? Currency,
    DateOnly? ValidUntil,
    int? PaymentTermsDays,
    int? DeliveryDays,
    IReadOnlyList<ExtractedLine> Lines,
    IReadOnlyList<string> Unknowns,
    AiCall Call);

public sealed record ExtractedLine(
    string RawText,
    string? SupplierCode,
    string? ManufacturerPartNumber,
    string? Description,
    decimal? Quantity,
    decimal? UnitPrice,
    string? Unit);

/// <summary>One call to a model: what went in, what came out, what it cost. The audit trail.</summary>
public sealed record AiCall(
    Guid Id,
    string Purpose,
    string InstructionsVersion,
    string Model,
    string Input,
    string RawOutput,
    long InputTokens,
    long OutputTokens,
    TimeSpan Duration,
    DateTimeOffset At);
