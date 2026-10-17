using System.Globalization;
using DotnetLeet.Trade.Domain.Catalog;
using DotnetLeet.Trade.Domain.Procurement;

namespace DotnetLeet.Trade.Web;

/// <summary>How a value shows on screen. A missing value is a dash, never an empty surprise.</summary>
public static class Fmt
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Money(decimal? value) => value is { } v ? v.ToString("N2", Inv) : "-";

    public static string Quantity(decimal? value) => value is { } v ? v.ToString("0.##", Inv) : "-";

    public static string Int(int? value) => value is { } v ? v.ToString(Inv) : "-";

    public static string Long(long value) => value.ToString("N0", Inv);

    public static string Text(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    public static string Date(DateOnly? value) => value is { } v ? v.ToString("yyyy-MM-dd", Inv) : "-";

    public static string When(DateTimeOffset? value) => value is { } v ? v.ToString("yyyy-MM-dd HH:mm", Inv) : "-";

    public static string Duration(TimeSpan value) =>
        value.TotalSeconds >= 1 ? $"{value.TotalSeconds.ToString("0.0", Inv)} s" : $"{value.TotalMilliseconds.ToString("0", Inv)} ms";

    public static string InputValue(decimal? value) => value is { } v ? v.ToString(Inv) : "";

    public static string InputValue(int? value) => value is { } v ? v.ToString(Inv) : "";

    public static string InputValue(DateOnly? value) => value is { } v ? v.ToString("yyyy-MM-dd", Inv) : "";

    public static decimal? ParseDecimal(object? raw) =>
        decimal.TryParse(raw?.ToString(), NumberStyles.Number, Inv, out var v) ? v : null;

    public static int? ParseInt(object? raw) =>
        int.TryParse(raw?.ToString(), NumberStyles.Integer, Inv, out var v) ? v : null;

    public static DateOnly? ParseDate(object? raw) =>
        DateOnly.TryParseExact(raw?.ToString(), "yyyy-MM-dd", Inv, DateTimeStyles.None, out var v) ? v : null;

    public static string MatchMethodLabel(MatchMethod method) => method switch
    {
        MatchMethod.None => "none",
        MatchMethod.Sku => "SKU",
        MatchMethod.SupplierCode => "supplier code",
        MatchMethod.ManufacturerPartNumber => "MPN",
        MatchMethod.Ean => "EAN",
        _ => method.ToString()
    };

    public static string ApprovalLabel(ApprovalLevel level) => level switch
    {
        ApprovalLevel.None => "none",
        _ => level.ToString()
    };

    public static string StatusClass(OfferStatus status) => status switch
    {
        OfferStatus.Draft => "text-bg-secondary",
        OfferStatus.Reviewed => "text-bg-success",
        OfferStatus.Rejected => "text-bg-dark",
        _ => "text-bg-light"
    };

    public static string IssueClass(IssueSeverity severity) =>
        severity == IssueSeverity.Blocking ? "issue-blocking" : "issue-warning";

    /// <summary>One sentence for the screen, whatever went wrong between here and the API.</summary>
    public static string Error(Exception ex) => ex switch
    {
        TradeApiException api => api.Message,
        HttpRequestException http => $"Could not reach the API: {http.Message}",
        TaskCanceledException or TimeoutException or Polly.Timeout.TimeoutRejectedException => "The API did not answer in time.",
        _ => $"{ex.GetType().Name}: {ex.Message}"
    };
}
