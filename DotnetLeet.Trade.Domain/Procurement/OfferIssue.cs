namespace DotnetLeet.Trade.Domain.Procurement;

public enum IssueSeverity
{
    /// <summary>The offer cannot be marked as reviewed until a person fixes this.</summary>
    Blocking,
    /// <summary>Worth a look, does not stop the review.</summary>
    Warning
}

public enum IssueCode
{
    UnknownSupplier,
    UnmatchedProduct,
    MissingQuantity,
    MissingUnitPrice,
    MissingCurrency,
    CurrencyMismatch,
    PriceDeviation,
    MissingValidity,
    OfferExpired,
    DuplicateLine,
    UnitMismatch,
    NoLines
}

public sealed record OfferIssue(IssueCode Code, IssueSeverity Severity, string Message)
{
    public static OfferIssue Blocking(IssueCode code, string message) => new(code, IssueSeverity.Blocking, message);
    public static OfferIssue Warning(IssueCode code, string message) => new(code, IssueSeverity.Warning, message);
}
