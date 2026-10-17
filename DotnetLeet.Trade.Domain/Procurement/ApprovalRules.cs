namespace DotnetLeet.Trade.Domain.Procurement;

public enum ApprovalLevel
{
    None,
    Manager,
    Director
}

/// <summary>
/// Who has to approve a purchase of a given size. Thresholds are in the company's
/// base currency. The rule is the company's and no model gets to change it.
/// </summary>
public static class ApprovalRules
{
    public const string BaseCurrency = "EUR";
    public const decimal ManagerThreshold = 1_000m;
    public const decimal DirectorThreshold = 10_000m;

    public static ApprovalLevel RequiredFor(decimal totalInBaseCurrency) => totalInBaseCurrency switch
    {
        < 0 => throw new ArgumentOutOfRangeException(nameof(totalInBaseCurrency)),
        < ManagerThreshold => ApprovalLevel.None,
        < DirectorThreshold => ApprovalLevel.Manager,
        _ => ApprovalLevel.Director
    };
}
