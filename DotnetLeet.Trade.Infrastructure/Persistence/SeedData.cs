using DotnetLeet.Trade.Domain.Catalog;
using DotnetLeet.Trade.Domain.Inventory;
using DotnetLeet.Trade.Domain.Procurement;

namespace DotnetLeet.Trade.Infrastructure.Persistence;

/// <summary>
/// The business foundation of level 0: a catalog of computer hardware, five suppliers
/// with their own article codes, what we paid last time, what is on the shelf, and two
/// open purchase requests. The evals use the same data, so an eval runs against the
/// catalog the app runs against.
/// </summary>
public sealed class SeedData
{
    public IReadOnlyList<Supplier> Suppliers { get; }
    public IReadOnlyList<Product> Products { get; }
    public IReadOnlyList<PurchasePriceHistory> PurchasePrices { get; }
    public IReadOnlyList<StockLevel> StockLevels { get; }
    public IReadOnlyList<PurchaseRequest> PurchaseRequests { get; }

    public Supplier Supplier(string code) => Suppliers.Single(s => s.Code == code);
    public Product Product(string sku) => Products.Single(p => p.Sku == sku);

    public static SeedData Create(DateOnly today)
    {
        var suppliers = new[]
        {
            Domain.Catalog.Supplier.Create("NORTHGATE", "Northgate Distribution GmbH", "EUR", 30, "northgate-dist.example"),
            Domain.Catalog.Supplier.Create("BALTIC", "Baltic Components OU", "EUR", 14, "balticcomponents.example"),
            Domain.Catalog.Supplier.Create("HELIX", "Helix IT Supply Ltd", "GBP", 30, "helixitsupply.example"),
            Domain.Catalog.Supplier.Create("WESTPORT", "Westport Electronics Inc", "USD", 45, "westport-electronics.example"),
            Domain.Catalog.Supplier.Create("ADRIA", "Adria Komponente d.o.o.", "EUR", 15, "adria-komponente.example")
        };

        var northgate = suppliers[0].Id;
        var baltic = suppliers[1].Id;
        var helix = suppliers[2].Id;
        var westport = suppliers[3].Id;
        var adria = suppliers[4].Id;

        var products = new List<Product>
        {
            // storage
            Domain.Catalog.Product.Create("SSD-SAM-990P-1T", "Samsung 990 PRO 1TB NVMe M.2 SSD", "Samsung", "MZ-V9P1T0BW", ProductCategory.Storage)
                .WithAlias(AliasKind.SupplierCode, "NG-114422", northgate).WithAlias(AliasKind.SupplierCode, "SAM990P1T", baltic).WithAlias(AliasKind.Ean, "8806094215038"),
            Domain.Catalog.Product.Create("SSD-SAM-990P-2T", "Samsung 990 PRO 2TB NVMe M.2 SSD", "Samsung", "MZ-V9P2T0BW", ProductCategory.Storage)
                .WithAlias(AliasKind.SupplierCode, "NG-114423", northgate).WithAlias(AliasKind.SupplierCode, "SAM990P2T", baltic),
            Domain.Catalog.Product.Create("SSD-WD-SN580-1T", "WD Blue SN580 1TB NVMe M.2 SSD", "Western Digital", "WDS100T3B0E", ProductCategory.Storage)
                .WithAlias(AliasKind.SupplierCode, "NG-118801", northgate).WithAlias(AliasKind.SupplierCode, "HX-WD-SN580-1", helix),
            Domain.Catalog.Product.Create("SSD-WD-SN580-2T", "WD Blue SN580 2TB NVMe M.2 SSD", "Western Digital", "WDS200T3B0E", ProductCategory.Storage)
                .WithAlias(AliasKind.SupplierCode, "NG-118802", northgate),
            Domain.Catalog.Product.Create("SSD-CRU-P3P-1T", "Crucial P3 Plus 1TB NVMe M.2 SSD", "Crucial", "CT1000P3PSSD8", ProductCategory.Storage)
                .WithAlias(AliasKind.SupplierCode, "CRUP3P1T", baltic).WithAlias(AliasKind.SupplierCode, "WE-55201", westport),
            Domain.Catalog.Product.Create("SSD-CRU-P3P-2T", "Crucial P3 Plus 2TB NVMe M.2 SSD", "Crucial", "CT2000P3PSSD8", ProductCategory.Storage)
                .WithAlias(AliasKind.SupplierCode, "CRUP3P2T", baltic),
            Domain.Catalog.Product.Create("SSD-KIN-A400-480", "Kingston A400 480GB SATA 2.5 SSD", "Kingston", "SA400S37/480G", ProductCategory.Storage)
                .WithAlias(AliasKind.SupplierCode, "AK-10044", adria).WithAlias(AliasKind.SupplierCode, "NG-101210", northgate),
            Domain.Catalog.Product.Create("HDD-SEA-IW-4T", "Seagate IronWolf 4TB NAS HDD 3.5", "Seagate", "ST4000VN006", ProductCategory.Storage)
                .WithAlias(AliasKind.SupplierCode, "NG-120044", northgate).WithAlias(AliasKind.SupplierCode, "HX-SEA-IW4", helix),
            Domain.Catalog.Product.Create("HDD-WD-RED-6T", "WD Red Plus 6TB NAS HDD 3.5", "Western Digital", "WD60EFPX", ProductCategory.Storage)
                .WithAlias(AliasKind.SupplierCode, "NG-120071", northgate),
            Domain.Catalog.Product.Create("SSD-SAM-T7-2T", "Samsung T7 2TB Portable SSD USB-C", "Samsung", "MU-PC2T0T/WW", ProductCategory.Storage)
                .WithAlias(AliasKind.SupplierCode, "SAMT72T", baltic).WithAlias(AliasKind.AlternateMpn, "MU-PC2T0T/AM"),

            // memory
            Domain.Catalog.Product.Create("RAM-KIN-FB-16-5600", "Kingston FURY Beast 16GB DDR5-5600 CL40", "Kingston", "KF556C40BB-16", ProductCategory.Memory)
                .WithAlias(AliasKind.SupplierCode, "NG-131001", northgate).WithAlias(AliasKind.SupplierCode, "AK-20016", adria),
            Domain.Catalog.Product.Create("RAM-KIN-FB-32K-5600", "Kingston FURY Beast 32GB (2x16GB) DDR5-5600 CL40 Kit", "Kingston", "KF556C40BBK2-32", ProductCategory.Memory)
                .WithAlias(AliasKind.SupplierCode, "NG-131002", northgate).WithAlias(AliasKind.SupplierCode, "AK-20032", adria),
            Domain.Catalog.Product.Create("RAM-COR-VLP-32K-6000", "Corsair Vengeance 32GB (2x16GB) DDR5-6000 CL36 Kit", "Corsair", "CMK32GX5M2B6000C36", ProductCategory.Memory)
                .WithAlias(AliasKind.SupplierCode, "CORV32K6000", baltic).WithAlias(AliasKind.SupplierCode, "WE-61002", westport),
            Domain.Catalog.Product.Create("RAM-CRU-SO-16-5600", "Crucial 16GB DDR5-5600 SO-DIMM", "Crucial", "CT16G56C46S5", ProductCategory.Memory)
                .WithAlias(AliasKind.SupplierCode, "CRUSO16", baltic),
            Domain.Catalog.Product.Create("RAM-KIN-VR-16-3200", "Kingston ValueRAM 16GB DDR4-3200", "Kingston", "KVR32N22D8/16", ProductCategory.Memory)
                .WithAlias(AliasKind.SupplierCode, "NG-130016", northgate).WithAlias(AliasKind.SupplierCode, "AK-20416", adria),

            // cpu
            Domain.Catalog.Product.Create("CPU-AMD-R5-7600X", "AMD Ryzen 5 7600X Box", "AMD", "100-100000593WOF", ProductCategory.Cpu)
                .WithAlias(AliasKind.SupplierCode, "NG-140593", northgate).WithAlias(AliasKind.SupplierCode, "WE-70593", westport),
            Domain.Catalog.Product.Create("CPU-AMD-R7-7800X3D", "AMD Ryzen 7 7800X3D Box", "AMD", "100-100000910WOF", ProductCategory.Cpu)
                .WithAlias(AliasKind.SupplierCode, "NG-140910", northgate).WithAlias(AliasKind.SupplierCode, "WE-70910", westport),
            Domain.Catalog.Product.Create("CPU-INT-I5-14400", "Intel Core i5-14400 Box", "Intel", "BX8071514400", ProductCategory.Cpu)
                .WithAlias(AliasKind.SupplierCode, "NG-141440", northgate).WithAlias(AliasKind.SupplierCode, "HX-INT-14400", helix),
            Domain.Catalog.Product.Create("CPU-INT-I7-14700K", "Intel Core i7-14700K Box", "Intel", "BX8071514700K", ProductCategory.Cpu)
                .WithAlias(AliasKind.SupplierCode, "NG-141470", northgate),

            // gpu
            Domain.Catalog.Product.Create("GPU-ASUS-4070S-12", "ASUS Dual GeForce RTX 4070 SUPER 12GB", "ASUS", "DUAL-RTX4070S-12G", ProductCategory.Gpu)
                .WithAlias(AliasKind.SupplierCode, "NG-150470", northgate).WithAlias(AliasKind.SupplierCode, "WE-80470", westport),
            Domain.Catalog.Product.Create("GPU-MSI-4060-8", "MSI GeForce RTX 4060 VENTUS 2X 8GB", "MSI", "RTX 4060 VENTUS 2X BLACK 8G OC", ProductCategory.Gpu)
                .WithAlias(AliasKind.SupplierCode, "NG-150460", northgate).WithAlias(AliasKind.SupplierCode, "AK-30460", adria),
            Domain.Catalog.Product.Create("GPU-SAP-7800XT-16", "Sapphire PULSE Radeon RX 7800 XT 16GB", "Sapphire", "11330-02-20G", ProductCategory.Gpu)
                .WithAlias(AliasKind.SupplierCode, "WE-80780", westport),

            // monitors
            Domain.Catalog.Product.Create("MON-DELL-U2723QE", "Dell UltraSharp U2723QE 27 4K USB-C Monitor", "Dell", "U2723QE", ProductCategory.Monitor)
                .WithAlias(AliasKind.SupplierCode, "NG-160723", northgate).WithAlias(AliasKind.SupplierCode, "HX-DELL-U2723", helix),
            Domain.Catalog.Product.Create("MON-DELL-P2422H", "Dell P2422H 24 FHD Monitor", "Dell", "P2422H", ProductCategory.Monitor)
                .WithAlias(AliasKind.SupplierCode, "NG-160422", northgate).WithAlias(AliasKind.SupplierCode, "AK-40422", adria),
            Domain.Catalog.Product.Create("MON-LG-27UP850", "LG 27UP850N-W 27 4K USB-C Monitor", "LG", "27UP850N-W", ProductCategory.Monitor)
                .WithAlias(AliasKind.SupplierCode, "LG27UP850", baltic).WithAlias(AliasKind.AlternateMpn, "27UP850-W"),
            Domain.Catalog.Product.Create("MON-SAM-S24-FHD", "Samsung S24C310 24 FHD Monitor", "Samsung", "LS24C310EAUXEN", ProductCategory.Monitor)
                .WithAlias(AliasKind.SupplierCode, "SAMS24C310", baltic).WithAlias(AliasKind.SupplierCode, "AK-40310", adria),

            // laptops
            Domain.Catalog.Product.Create("NB-LEN-T14-G5", "Lenovo ThinkPad T14 Gen 5 14 Ryzen 7 32GB 1TB", "Lenovo", "21MC0017GE", ProductCategory.Laptop)
                .WithAlias(AliasKind.SupplierCode, "NG-170014", northgate).WithAlias(AliasKind.SupplierCode, "HX-LEN-T14G5", helix),
            Domain.Catalog.Product.Create("NB-DELL-LAT-5550", "Dell Latitude 5550 15 Core Ultra 5 16GB 512GB", "Dell", "N001L555015EMEA", ProductCategory.Laptop)
                .WithAlias(AliasKind.SupplierCode, "NG-170550", northgate),
            Domain.Catalog.Product.Create("NB-HP-EB-840-G11", "HP EliteBook 840 G11 14 Core Ultra 7 16GB 512GB", "HP", "9G0Z4ET", ProductCategory.Laptop)
                .WithAlias(AliasKind.SupplierCode, "NG-170840", northgate).WithAlias(AliasKind.SupplierCode, "HX-HP-840G11", helix),
            Domain.Catalog.Product.Create("NB-APP-MBA-M3-13", "Apple MacBook Air 13 M3 16GB 256GB", "Apple", "MXCR3D/A", ProductCategory.Laptop)
                .WithAlias(AliasKind.SupplierCode, "WE-90013", westport).WithAlias(AliasKind.AlternateMpn, "MXCR3LL/A"),

            // networking
            Domain.Catalog.Product.Create("NET-UBQ-USW-24", "Ubiquiti UniFi Switch 24 PoE", "Ubiquiti", "USW-24-POE", ProductCategory.Networking)
                .WithAlias(AliasKind.SupplierCode, "WE-95024", westport).WithAlias(AliasKind.SupplierCode, "NG-180024", northgate),
            Domain.Catalog.Product.Create("NET-UBQ-U6-PRO", "Ubiquiti UniFi U6 Pro Access Point", "Ubiquiti", "U6-PRO", ProductCategory.Networking)
                .WithAlias(AliasKind.SupplierCode, "WE-95006", westport),
            Domain.Catalog.Product.Create("NET-TPL-SG108", "TP-Link TL-SG108 8-Port Gigabit Switch", "TP-Link", "TL-SG108", ProductCategory.Networking)
                .WithAlias(AliasKind.SupplierCode, "AK-50108", adria).WithAlias(AliasKind.SupplierCode, "NG-180108", northgate),
            Domain.Catalog.Product.Create("NET-MIK-HAP-AX3", "MikroTik hAP ax3 Router", "MikroTik", "C53UIG+5HPAXD2HPAXD", ProductCategory.Networking)
                .WithAlias(AliasKind.SupplierCode, "AK-50003", adria),

            // peripherals
            Domain.Catalog.Product.Create("PER-LOG-MX3S", "Logitech MX Master 3S Mouse", "Logitech", "910-006559", ProductCategory.Peripheral)
                .WithAlias(AliasKind.SupplierCode, "NG-190559", northgate).WithAlias(AliasKind.SupplierCode, "AK-60559", adria),
            Domain.Catalog.Product.Create("PER-LOG-MXKEYS", "Logitech MX Keys S Keyboard", "Logitech", "920-011587", ProductCategory.Peripheral)
                .WithAlias(AliasKind.SupplierCode, "NG-190587", northgate),
            Domain.Catalog.Product.Create("PER-LOG-C920", "Logitech C920 HD Pro Webcam", "Logitech", "960-001055", ProductCategory.Peripheral)
                .WithAlias(AliasKind.SupplierCode, "LOGC920", baltic).WithAlias(AliasKind.SupplierCode, "AK-60920", adria),
            Domain.Catalog.Product.Create("PER-DELL-WD22TB4", "Dell WD22TB4 Thunderbolt 4 Dock", "Dell", "WD22TB4", ProductCategory.Peripheral)
                .WithAlias(AliasKind.SupplierCode, "NG-190224", northgate).WithAlias(AliasKind.SupplierCode, "HX-DELL-WD22", helix),
            Domain.Catalog.Product.Create("PER-APC-BX750", "APC Back-UPS 750VA BX750MI", "APC", "BX750MI", ProductCategory.Peripheral)
                .WithAlias(AliasKind.SupplierCode, "AK-60750", adria),

            // cables, sold per pack of 10
            Domain.Catalog.Product.Create("CAB-CAT6-2M-10", "Cat6 UTP Patch Cable 2m, pack of 10", "Generic", "CAT6-UTP-2M-10PK", ProductCategory.Cable, Unit.Pack)
                .WithAlias(AliasKind.SupplierCode, "AK-70002", adria).WithAlias(AliasKind.SupplierCode, "NG-199002", northgate),
            Domain.Catalog.Product.Create("CAB-HDMI-2M-10", "HDMI 2.1 Cable 2m, pack of 10", "Generic", "HDMI21-2M-10PK", ProductCategory.Cable, Unit.Pack)
                .WithAlias(AliasKind.SupplierCode, "AK-70012", adria),
            Domain.Catalog.Product.Create("CAB-USBC-1M-10", "USB-C to USB-C 100W Cable 1m, pack of 10", "Generic", "USBC-100W-1M-10PK", ProductCategory.Cable, Unit.Pack)
                .WithAlias(AliasKind.SupplierCode, "AK-70021", adria).WithAlias(AliasKind.SupplierCode, "LOGUSBC1M10", baltic)
        };

        Product P(string sku) => products.Single(p => p.Sku == sku);

        var threeMonthsAgo = today.AddMonths(-3);
        var sixWeeksAgo = today.AddDays(-42);

        var prices = new List<PurchasePriceHistory>
        {
            PurchasePriceHistory.Record(P("SSD-SAM-990P-1T").Id, northgate, 98.50m, "EUR", threeMonthsAgo),
            PurchasePriceHistory.Record(P("SSD-SAM-990P-1T").Id, baltic, 96.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("SSD-SAM-990P-2T").Id, northgate, 171.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("SSD-WD-SN580-1T").Id, northgate, 58.90m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("SSD-WD-SN580-2T").Id, northgate, 104.00m, "EUR", threeMonthsAgo),
            PurchasePriceHistory.Record(P("SSD-CRU-P3P-1T").Id, baltic, 54.20m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("SSD-KIN-A400-480").Id, adria, 29.90m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("HDD-SEA-IW-4T").Id, northgate, 109.00m, "EUR", threeMonthsAgo),
            PurchasePriceHistory.Record(P("HDD-WD-RED-6T").Id, northgate, 158.00m, "EUR", threeMonthsAgo),
            PurchasePriceHistory.Record(P("RAM-KIN-FB-16-5600").Id, northgate, 44.80m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("RAM-KIN-FB-32K-5600").Id, adria, 87.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("RAM-COR-VLP-32K-6000").Id, baltic, 101.00m, "EUR", threeMonthsAgo),
            PurchasePriceHistory.Record(P("RAM-KIN-VR-16-3200").Id, adria, 31.50m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("CPU-AMD-R5-7600X").Id, northgate, 189.00m, "EUR", threeMonthsAgo),
            PurchasePriceHistory.Record(P("CPU-AMD-R7-7800X3D").Id, northgate, 349.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("CPU-INT-I5-14400").Id, northgate, 198.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("CPU-INT-I7-14700K").Id, northgate, 362.00m, "EUR", threeMonthsAgo),
            PurchasePriceHistory.Record(P("GPU-ASUS-4070S-12").Id, northgate, 579.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("GPU-MSI-4060-8").Id, adria, 279.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("MON-DELL-U2723QE").Id, northgate, 489.00m, "EUR", threeMonthsAgo),
            PurchasePriceHistory.Record(P("MON-DELL-P2422H").Id, adria, 139.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("MON-LG-27UP850").Id, baltic, 359.00m, "EUR", threeMonthsAgo),
            PurchasePriceHistory.Record(P("MON-SAM-S24-FHD").Id, adria, 94.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("NB-LEN-T14-G5").Id, northgate, 1_289.00m, "EUR", threeMonthsAgo),
            PurchasePriceHistory.Record(P("NB-DELL-LAT-5550").Id, northgate, 1_095.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("NB-HP-EB-840-G11").Id, northgate, 1_340.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("NET-UBQ-USW-24").Id, westport, 349.00m, "USD", threeMonthsAgo),
            PurchasePriceHistory.Record(P("NET-UBQ-U6-PRO").Id, westport, 139.00m, "USD", sixWeeksAgo),
            PurchasePriceHistory.Record(P("NET-TPL-SG108").Id, adria, 18.40m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("PER-LOG-MX3S").Id, northgate, 79.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("PER-LOG-MXKEYS").Id, northgate, 89.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("PER-LOG-C920").Id, adria, 58.00m, "EUR", threeMonthsAgo),
            PurchasePriceHistory.Record(P("PER-DELL-WD22TB4").Id, northgate, 219.00m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("CAB-CAT6-2M-10").Id, adria, 14.90m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("CAB-HDMI-2M-10").Id, adria, 24.50m, "EUR", sixWeeksAgo),
            PurchasePriceHistory.Record(P("CAB-USBC-1M-10").Id, adria, 39.00m, "EUR", sixWeeksAgo)
        };

        var random = new Random(20261008);
        var stock = products.Select(p => StockLevel.Start(p.Id, random.Next(0, 40))).ToList();

        var requests = new List<PurchaseRequest>
        {
            PurchaseRequest.Open("PR-2026-0017", "Sales", "Q4 workstation refresh for two customers", new DateTimeOffset(today.AddDays(-5), TimeOnly.MinValue, TimeSpan.Zero))
                .WithLine(P("SSD-SAM-990P-1T").Id, 20)
                .WithLine(P("RAM-KIN-FB-32K-5600").Id, 20)
                .WithLine(P("CPU-AMD-R5-7600X").Id, 10),
            PurchaseRequest.Open("PR-2026-0018", "Service desk", "Replacement monitors and docks for the office", new DateTimeOffset(today.AddDays(-2), TimeOnly.MinValue, TimeSpan.Zero))
                .WithLine(P("MON-DELL-U2723QE").Id, 6)
                .WithLine(P("PER-DELL-WD22TB4").Id, 6)
        };

        return new SeedData(suppliers, products, prices, stock, requests);
    }

    private SeedData(
        IReadOnlyList<Supplier> suppliers,
        IReadOnlyList<Product> products,
        IReadOnlyList<PurchasePriceHistory> prices,
        IReadOnlyList<StockLevel> stock,
        IReadOnlyList<PurchaseRequest> requests)
    {
        Suppliers = suppliers;
        Products = products;
        PurchasePrices = prices;
        StockLevels = stock;
        PurchaseRequests = requests;
    }
}
