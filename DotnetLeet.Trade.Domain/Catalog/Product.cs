namespace DotnetLeet.Trade.Domain.Catalog;

public enum ProductCategory
{
    Storage,
    Memory,
    Cpu,
    Gpu,
    Monitor,
    Laptop,
    Networking,
    Peripheral,
    Cable
}

public enum Unit
{
    Piece,
    Pack
}

/// <summary>
/// A product we sell and buy. Our own SKU is the identity. Suppliers call it
/// by their own codes, which live in <see cref="ProductAlias"/>.
/// </summary>
public sealed class Product
{
    public Guid Id { get; private set; }
    public string Sku { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Manufacturer { get; private set; } = "";
    public string ManufacturerPartNumber { get; private set; } = "";
    public ProductCategory Category { get; private set; }
    public Unit Unit { get; private set; }
    public bool Active { get; private set; } = true;

    private readonly List<ProductAlias> _aliases = [];
    public IReadOnlyCollection<ProductAlias> Aliases => _aliases;

    private Product() { }

    public static Product Create(string sku, string name, string manufacturer, string mpn, ProductCategory category, Unit unit = Unit.Piece)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("A product needs a SKU.", nameof(sku));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A product needs a name.", nameof(name));

        return new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Manufacturer = manufacturer.Trim(),
            ManufacturerPartNumber = mpn.Trim().ToUpperInvariant(),
            Category = category,
            Unit = unit
        };
    }

    public Product WithAlias(AliasKind kind, string code, Guid? supplierId = null)
    {
        _aliases.Add(ProductAlias.Create(Id, kind, code, supplierId));
        return this;
    }
}

public enum AliasKind
{
    /// <summary>The article code one supplier uses in its offers.</summary>
    SupplierCode,
    /// <summary>Another manufacturer part number for the same product (regional variants, repackaging).</summary>
    AlternateMpn,
    Ean
}

public sealed class ProductAlias
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public AliasKind Kind { get; private set; }
    public string Code { get; private set; } = "";

    private ProductAlias() { }

    public static ProductAlias Create(Guid productId, AliasKind kind, string code, Guid? supplierId)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("An alias needs a code.", nameof(code));
        if (kind == AliasKind.SupplierCode && supplierId is null) throw new ArgumentException("A supplier code belongs to a supplier.", nameof(supplierId));

        return new ProductAlias
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            SupplierId = supplierId,
            Kind = kind,
            Code = ProductCodes.Normalize(code)
        };
    }
}

/// <summary>
/// Codes come in as "WDS100T3B0A", "wds-100t3b0a" or "WDS 100T 3B0A". One shape for all of them.
/// </summary>
public static class ProductCodes
{
    public static string Normalize(string code) =>
        new(code.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
