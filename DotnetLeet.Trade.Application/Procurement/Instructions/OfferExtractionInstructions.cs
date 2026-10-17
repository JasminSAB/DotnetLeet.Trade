using System.Reflection;

namespace DotnetLeet.Trade.Application.Procurement.Instructions;

/// <summary>
/// The rules the model follows when it reads a supplier email. Prose, versioned,
/// embedded in the application assembly. Not a string constant in the adapter,
/// not a config value, not assembled at runtime.
/// </summary>
public static class OfferExtractionInstructions
{
    /// <summary>
    /// v1: the first draft. v2: dates always as yyyy-MM-dd, currency from symbols and per-line codes,
    /// numbers copied digit for digit, tier lines repeat the product. v3: availability ("in stock",
    /// "ships within 24 hours") is not a delivery time and leaves the field empty. Each change came from a failed eval.
    /// </summary>
    public const string Version = "offer-extraction-v3";

    private static readonly Lazy<string> Loaded = new(Load);

    public static string Text => Loaded.Value;

    private static string Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith($"{Version}.md", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
