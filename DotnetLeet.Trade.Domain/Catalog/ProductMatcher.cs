namespace DotnetLeet.Trade.Domain.Catalog;

public enum MatchMethod
{
    None,
    /// <summary>The supplier wrote our own SKU.</summary>
    Sku,
    /// <summary>The supplier's own article code, known from an earlier offer.</summary>
    SupplierCode,
    /// <summary>The manufacturer part number, ours or an alternate.</summary>
    ManufacturerPartNumber,
    Ean,
    /// <summary>A known code found inside the line text, when the extracted fields had none.</summary>
    CodeInLine
}

public sealed record ProductMatch(Guid? ProductId, MatchMethod Method)
{
    public static readonly ProductMatch None = new(null, MatchMethod.None);
    public bool Found => ProductId is not null;
}

/// <summary>
/// Deterministic matching of a supplier's line to our catalog. Codes only, no guessing.
/// A line that matches nothing stays unmatched and a person decides. Fuzzy matching
/// by description is a later level, and it will be a proposal, never a match.
/// </summary>
public sealed class ProductMatcher
{
    /// <summary>Shorter codes are too easy to find by accident inside unrelated text.</summary>
    public const int MinCodeLengthInText = 6;

    private readonly Dictionary<string, Guid> _bySku;
    private readonly Dictionary<string, Guid> _byMpn;
    private readonly Dictionary<(Guid SupplierId, string Code), Guid> _bySupplierCode;
    private readonly Dictionary<string, Guid> _byEan;

    public ProductMatcher(IEnumerable<Product> products)
    {
        var list = products.ToList();

        _bySku = list.ToDictionary(p => ProductCodes.Normalize(p.Sku), p => p.Id);

        _byMpn = new Dictionary<string, Guid>();
        foreach (var p in list)
            _byMpn.TryAdd(ProductCodes.Normalize(p.ManufacturerPartNumber), p.Id);
        foreach (var alias in list.SelectMany(p => p.Aliases).Where(a => a.Kind == AliasKind.AlternateMpn))
            _byMpn.TryAdd(alias.Code, alias.ProductId);

        _bySupplierCode = list.SelectMany(p => p.Aliases)
            .Where(a => a.Kind == AliasKind.SupplierCode && a.SupplierId is not null)
            .GroupBy(a => (a.SupplierId!.Value, a.Code))
            .ToDictionary(g => g.Key, g => g.First().ProductId);

        _byEan = list.SelectMany(p => p.Aliases)
            .Where(a => a.Kind == AliasKind.Ean)
            .GroupBy(a => a.Code)
            .ToDictionary(g => g.Key, g => g.First().ProductId);
    }

    /// <summary>
    /// Tries the extracted codes first, in the order SKU, supplier code, part number, EAN.
    /// When none of them is known, looks for any known code inside the raw line text.
    /// </summary>
    public ProductMatch Match(Guid? supplierId, string? supplierCode, string? mpn, string? ean = null, string? rawText = null)
    {
        foreach (var candidate in new[] { supplierCode, mpn, ean }.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => ProductCodes.Normalize(c!)))
        {
            var match = Lookup(supplierId, candidate);
            if (match.Found)
                return match;
        }

        if (!string.IsNullOrWhiteSpace(rawText))
            return InText(supplierId, ProductCodes.Normalize(rawText));

        return ProductMatch.None;
    }

    private ProductMatch Lookup(Guid? supplierId, string candidate)
    {
        if (_bySku.TryGetValue(candidate, out var bySku))
            return new ProductMatch(bySku, MatchMethod.Sku);

        if (supplierId is not null && _bySupplierCode.TryGetValue((supplierId.Value, candidate), out var bySupplier))
            return new ProductMatch(bySupplier, MatchMethod.SupplierCode);

        if (_byMpn.TryGetValue(candidate, out var byMpn))
            return new ProductMatch(byMpn, MatchMethod.ManufacturerPartNumber);

        if (_byEan.TryGetValue(candidate, out var byEan))
            return new ProductMatch(byEan, MatchMethod.Ean);

        return ProductMatch.None;
    }

    private ProductMatch InText(Guid? supplierId, string normalizedText)
    {
        var codes = _bySku.Select(kv => new KeyValuePair<string, Guid>(kv.Key, kv.Value))
            .Concat(_byMpn.Select(kv => new KeyValuePair<string, Guid>(kv.Key, kv.Value)))
            .Concat(_byEan.Select(kv => new KeyValuePair<string, Guid>(kv.Key, kv.Value)))
            .Concat(_bySupplierCode.Where(kv => kv.Key.SupplierId == supplierId).Select(kv => new KeyValuePair<string, Guid>(kv.Key.Code, kv.Value)))
            .Where(c => c.Key.Length >= MinCodeLengthInText)
            .OrderByDescending(c => c.Key.Length);

        foreach (var code in codes)
        {
            if (normalizedText.Contains(code.Key, StringComparison.Ordinal))
                return new ProductMatch(code.Value, MatchMethod.CodeInLine);
        }

        return ProductMatch.None;
    }
}
