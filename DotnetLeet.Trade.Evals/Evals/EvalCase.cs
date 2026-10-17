using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DotnetLeet.Trade.Application.Procurement.Queries;

namespace DotnetLeet.Trade.Evals.Evals;

/// <summary>
/// One supplier email and what a careful person would extract from it. The expected
/// values were written by a person before any model saw the email. Programmatic checks
/// only at level 1: exact field matches, no judge.
/// </summary>
public sealed record EvalCase(string Name, string SenderAddress, string Email, ExpectedOffer Expected, string Notes)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static IReadOnlyList<EvalCase> LoadAll()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Evals", "cases");
        if (!Directory.Exists(folder))
            return [];

        return Directory.GetFiles(folder, "*.json")
            .OrderBy(f => f)
            .Select(f => JsonSerializer.Deserialize<EvalCase>(File.ReadAllText(f), Json)!)
            .ToList();
    }

    /// <summary>Every field is one check. The report counts fields, not only cases.</summary>
    public EvalScore Score(OfferDetail offer, string? resolvedSupplierCode)
    {
        var checks = new List<FieldCheck>
        {
            new("supplier", Expected.SupplierCode, resolvedSupplierCode),
            new("supplierReference", Expected.SupplierReference, offer.SupplierReference),
            new("currency", Expected.Currency, offer.Currency),
            new("validUntil", Expected.ValidUntil?.ToString("yyyy-MM-dd"), offer.ValidUntil?.ToString("yyyy-MM-dd")),
            new("paymentTermsDays", Expected.PaymentTermsDays?.ToString(), offer.PaymentTermsDays?.ToString()),
            new("deliveryDays", Expected.DeliveryDays?.ToString(), offer.DeliveryDays?.ToString()),
            new("lineCount", Expected.Lines.Count.ToString(), offer.Lines.Count.ToString()),
            new("unpricedLines", Expected.UnpricedLines.ToString(), offer.UnpricedLines.ToString())
        };

        for (var i = 0; i < Expected.Lines.Count; i++)
        {
            var expected = Expected.Lines[i];
            var actual = i < offer.Lines.Count ? offer.Lines[i] : null;
            var prefix = $"line{i + 1}.";

            checks.Add(new(prefix + "sku", expected.Sku, actual?.ProductSku));
            checks.Add(new(prefix + "quantity", Fmt(expected.Quantity), Fmt(actual?.Quantity)));
            checks.Add(new(prefix + "unitPrice", Fmt(expected.UnitPrice), Fmt(actual?.UnitPrice)));
        }

        // Invented numbers: anything the model computed or made up shows up as a number that is not in the email.
        var emailNumbers = Numbers(Email);
        var outputNumbers = offer.Lines
            .SelectMany(l => new[] { l.Quantity, l.UnitPrice })
            .Where(n => n is not null)
            .Select(n => Fmt(n)!)
            .Distinct()
            .ToList();
        var invented = outputNumbers.Where(n => !emailNumbers.Contains(n)).ToList();
        checks.Add(new("noInventedNumbers", "", string.Join(", ", invented)));

        var forbidden = Expected.MustNotAppear
            .Where(f => outputNumbers.Contains(Fmt(decimal.Parse(f, System.Globalization.CultureInfo.InvariantCulture))!))
            .ToList();
        checks.Add(new("noComputedNumbers", "", string.Join(", ", forbidden)));

        return new EvalScore(Name, checks);
    }

    private static string? Fmt(decimal? value) => value is null ? null : value.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Numbers as they appear in the email, normalized: "1,234.00", "1234" and "1234.0" are the same number,
    /// and "168,40" (European decimal comma) is 168.4. Whitespace never joins two numbers.
    /// </summary>
    private static HashSet<string> Numbers(string text)
    {
        var set = new HashSet<string>();
        foreach (Match m in Regex.Matches(text, @"\d+(?:[.,]\d+)*"))
        {
            var raw = m.Value;
            foreach (var candidate in new[] { raw.Replace(",", ""), raw.Replace(".", "").Replace(",", "."), raw })
            {
                if (decimal.TryParse(candidate, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d))
                    set.Add(Fmt(d)!);
            }
        }
        return set;
    }
}

public sealed record ExpectedOffer(
    string? SupplierCode,
    string? SupplierReference,
    string? Currency,
    DateOnly? ValidUntil,
    int? PaymentTermsDays,
    int? DeliveryDays,
    IReadOnlyList<ExpectedLine> Lines,
    int UnpricedLines,
    IReadOnlyList<string> MustNotAppear);

public sealed record ExpectedLine(string? Sku, decimal? Quantity, decimal? UnitPrice, string? Unit);

public sealed record FieldCheck(string Field, string? Expected, string? Actual)
{
    public bool Passed => string.Equals(Expected ?? "", Actual ?? "", StringComparison.Ordinal);
}

public sealed record EvalScore(string Case, IReadOnlyList<FieldCheck> Checks)
{
    public int Passed => Checks.Count(c => c.Passed);
    public int Total => Checks.Count;
    public bool AllPassed => Passed == Total;
    public IEnumerable<FieldCheck> Failures => Checks.Where(c => !c.Passed);
}
