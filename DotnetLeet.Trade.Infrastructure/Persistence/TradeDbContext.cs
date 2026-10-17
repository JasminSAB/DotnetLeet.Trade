using System.Text.Json;
using DotnetLeet.Trade.Application.Procurement.Ports;
using DotnetLeet.Trade.Domain.Catalog;
using DotnetLeet.Trade.Domain.Inventory;
using DotnetLeet.Trade.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DotnetLeet.Trade.Infrastructure.Persistence;

/// <summary>
/// One database, one schema per module. Catalog, procurement and inventory never
/// join each other's tables; they share product ids and nothing else.
/// </summary>
public sealed class TradeDbContext(DbContextOptions<TradeDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierOffer> Offers => Set<SupplierOffer>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchasePriceHistory> PurchasePrices => Set<PurchasePriceHistory>();
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();
    public DbSet<AiCall> AiCalls => Set<AiCall>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // catalog
        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("products", "catalog");
            b.HasKey(p => p.Id);
            b.Property(p => p.Sku).HasMaxLength(64);
            b.HasIndex(p => p.Sku).IsUnique();
            b.Property(p => p.Name).HasMaxLength(256);
            b.Property(p => p.Manufacturer).HasMaxLength(128);
            b.Property(p => p.ManufacturerPartNumber).HasMaxLength(64);
            b.Property(p => p.Category).HasConversion<string>().HasMaxLength(32);
            b.Property(p => p.Unit).HasConversion<string>().HasMaxLength(16);
            b.HasMany(p => p.Aliases).WithOne().HasForeignKey(a => a.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(p => p.Aliases).HasField("_aliases").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ProductAlias>(b =>
        {
            b.ToTable("product_aliases", "catalog");
            b.HasKey(a => a.Id);
            b.Property(a => a.Kind).HasConversion<string>().HasMaxLength(32);
            b.Property(a => a.Code).HasMaxLength(64);
            b.HasIndex(a => new { a.Kind, a.Code, a.SupplierId });
        });

        modelBuilder.Entity<Supplier>(b =>
        {
            b.ToTable("suppliers", "catalog");
            b.HasKey(s => s.Id);
            b.Property(s => s.Code).HasMaxLength(32);
            b.HasIndex(s => s.Code).IsUnique();
            b.Property(s => s.Name).HasMaxLength(256);
            b.Property(s => s.Currency).HasMaxLength(3);
            b.Property(s => s.EmailDomain).HasMaxLength(128);
        });

        // procurement
        modelBuilder.Entity<SupplierOffer>(b =>
        {
            b.ToTable("supplier_offers", "procurement");
            b.HasKey(o => o.Id);
            b.Property(o => o.SourceHash).HasMaxLength(64);
            b.HasIndex(o => o.SourceHash).IsUnique();
            b.Property(o => o.Source).HasConversion<string>().HasMaxLength(16);
            b.Property(o => o.Status).HasConversion<string>().HasMaxLength(16);
            b.Property(o => o.Currency).HasMaxLength(3);
            b.Property(o => o.SupplierReference).HasMaxLength(128);
            b.Property(o => o.SenderAddress).HasMaxLength(256);
            b.Property(o => o.Subtotal).HasPrecision(18, 2);
            b.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OfferId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(o => o.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Ignore(o => o.Issues);
            b.Ignore(o => o.HasBlockingIssues);
            b.Property<List<OfferIssue>>("_issues").HasColumnName("Issues").HasColumnType("jsonb").HasConversion(IssuesConverter, IssuesComparer);
        });

        modelBuilder.Entity<OfferLine>(b =>
        {
            b.ToTable("supplier_offer_lines", "procurement");
            b.HasKey(l => l.Id);
            b.Property(l => l.SupplierCode).HasMaxLength(64);
            b.Property(l => l.ManufacturerPartNumber).HasMaxLength(64);
            b.Property(l => l.Description).HasMaxLength(512);
            b.Property(l => l.Unit).HasMaxLength(16);
            b.Property(l => l.Quantity).HasPrecision(18, 3);
            b.Property(l => l.UnitPrice).HasPrecision(18, 4);
            b.Property(l => l.LineTotal).HasPrecision(18, 2);
            b.Property(l => l.MatchMethod).HasConversion<string>().HasMaxLength(32);
            b.Ignore(l => l.Issues);
            b.Property<List<OfferIssue>>("_issues").HasColumnName("Issues").HasColumnType("jsonb").HasConversion(IssuesConverter, IssuesComparer);
        });

        modelBuilder.Entity<PurchaseRequest>(b =>
        {
            b.ToTable("purchase_requests", "procurement");
            b.HasKey(r => r.Id);
            b.Property(r => r.Number).HasMaxLength(32);
            b.HasIndex(r => r.Number).IsUnique();
            b.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
            b.HasMany(r => r.Lines).WithOne().HasForeignKey(l => l.PurchaseRequestId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(r => r.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<PurchaseRequestLine>(b =>
        {
            b.ToTable("purchase_request_lines", "procurement");
            b.HasKey(l => l.Id);
            b.Property(l => l.Quantity).HasPrecision(18, 3);
        });

        modelBuilder.Entity<PurchasePriceHistory>(b =>
        {
            b.ToTable("purchase_prices", "procurement");
            b.HasKey(p => p.Id);
            b.Property(p => p.UnitPrice).HasPrecision(18, 4);
            b.Property(p => p.Currency).HasMaxLength(3);
            b.HasIndex(p => new { p.ProductId, p.PurchasedAt });
        });

        // inventory
        modelBuilder.Entity<StockLevel>(b =>
        {
            b.ToTable("stock_levels", "inventory");
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.ProductId).IsUnique();
            b.Property(s => s.OnHand).HasPrecision(18, 3);
            b.Property(s => s.Reserved).HasPrecision(18, 3);
            b.Ignore(s => s.Available);
        });

        // ai audit trail
        modelBuilder.Entity<AiCall>(b =>
        {
            b.ToTable("calls", "ai");
            b.HasKey(c => c.Id);
            b.Property(c => c.Purpose).HasMaxLength(64);
            b.Property(c => c.InstructionsVersion).HasMaxLength(64);
            b.Property(c => c.Model).HasMaxLength(128);
            b.HasIndex(c => c.At);
        });
    }

    private static readonly JsonSerializerOptions IssuesJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static readonly ValueConverter<List<OfferIssue>, string> IssuesConverter = new(
        issues => JsonSerializer.Serialize(issues, IssuesJson),
        json => JsonSerializer.Deserialize<List<OfferIssue>>(json, IssuesJson) ?? new List<OfferIssue>());

    private static readonly ValueComparer<List<OfferIssue>> IssuesComparer = new(
        (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
        v => v.Aggregate(0, (hash, issue) => HashCode.Combine(hash, issue.GetHashCode())),
        v => v.ToList());
}
