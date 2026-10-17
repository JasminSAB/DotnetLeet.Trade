using DotnetLeet.Trade.Application.Procurement.ExtractOffer;
using DotnetLeet.Trade.Domain.Catalog;
using DotnetLeet.Trade.Domain.Procurement;
using DotnetLeet.Trade.Infrastructure.Persistence;

namespace DotnetLeet.Trade.Evals.Unit;

public sealed class DomainTests
{
    private static readonly DateOnly Today = new(2026, 10, 8);
    private static readonly SeedData Seed = SeedData.Create(Today);

    private static OfferContext Context(Supplier? supplier) => new(
        supplier,
        new ProductMatcher(Seed.Products),
        Seed.Products.ToDictionary(p => p.Id),
        Seed.PurchasePrices.GroupBy(p => p.ProductId).ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.PurchasedAt).First().ToLastPurchase()),
        Today);

    private static SupplierOffer DraftFor(Supplier? supplier) =>
        SupplierOffer.Draft(OfferSource.Email, "text", "hash", null, DateTimeOffset.UtcNow, supplier?.Id, null);

    [Theory]
    [InlineData("NORTHGATE", "NG-114422", null, "SSD-SAM-990P-1T", MatchMethod.SupplierCode)]
    [InlineData("BALTIC", null, "MZ-V9P1T0BW", "SSD-SAM-990P-1T", MatchMethod.ManufacturerPartNumber)]
    [InlineData("BALTIC", null, "mz v9p1t0bw", "SSD-SAM-990P-1T", MatchMethod.ManufacturerPartNumber)]
    [InlineData("HELIX", "SSD-SAM-990P-1T", null, "SSD-SAM-990P-1T", MatchMethod.Sku)]
    [InlineData("WESTPORT", null, "MU-PC2T0T/AM", "SSD-SAM-T7-2T", MatchMethod.ManufacturerPartNumber)]
    public void Matcher_finds_products_by_our_sku_supplier_code_and_part_number(string supplierCode, string? code, string? mpn, string expectedSku, MatchMethod expectedMethod)
    {
        var matcher = new ProductMatcher(Seed.Products);

        var match = matcher.Match(Seed.Supplier(supplierCode).Id, code, mpn);

        Assert.Equal(Seed.Product(expectedSku).Id, match.ProductId);
        Assert.Equal(expectedMethod, match.Method);
    }

    [Fact]
    public void Matcher_finds_a_known_code_inside_the_line_text_when_the_fields_are_empty()
    {
        var matcher = new ProductMatcher(Seed.Products);

        var byMpn = matcher.Match(null, null, null, rawText: "4 x 9G0Z4ET HP EliteBook 840 G11 14 inch   1,298.00");
        var bySupplierCode = matcher.Match(Seed.Supplier("HELIX").Id, null, null, rawText: "HX-HP-840G11   HP EliteBook 840 G11     4 pcs    1,298.00");
        var nothing = matcher.Match(null, null, null, rawText: "4 x HP EliteBook 840 G11");

        Assert.Equal(Seed.Product("NB-HP-EB-840-G11").Id, byMpn.ProductId);
        Assert.Equal(MatchMethod.CodeInLine, byMpn.Method);
        Assert.Equal(Seed.Product("NB-HP-EB-840-G11").Id, bySupplierCode.ProductId);
        Assert.False(nothing.Found);
    }

    [Fact]
    public void Matcher_does_not_use_another_suppliers_code()
    {
        var matcher = new ProductMatcher(Seed.Products);

        var match = matcher.Match(Seed.Supplier("BALTIC").Id, "NG-114422", null);

        Assert.False(match.Found);
    }

    [Fact]
    public void A_complete_offer_has_no_blocking_issues_and_the_arithmetic_is_ours()
    {
        var northgate = Seed.Supplier("NORTHGATE");
        var offer = DraftFor(northgate);
        offer.SetHeader("Q-1", "EUR", Today.AddDays(14), 30, 3);
        offer.AddLine("20 x NG-114422 Samsung 990 PRO 1TB @ 97.50", "NG-114422", null, "Samsung 990 PRO 1TB", 20, 97.50m, "pcs");
        offer.AddLine("10 x NG-131002 Kingston FURY 32GB kit @ 86.00", "NG-131002", null, "Kingston FURY 32GB", 10, 86.00m, "pcs");

        offer.Evaluate(Context(northgate));

        Assert.False(offer.HasBlockingIssues);
        Assert.Equal(1950.00m + 860.00m, offer.Subtotal);
        Assert.Equal(0, offer.UnpricedLines);
        Assert.All(offer.Lines, l => Assert.NotNull(l.ProductId));
    }

    [Fact]
    public void Missing_price_unknown_product_and_unknown_supplier_block_the_review()
    {
        var offer = DraftFor(null);
        offer.SetHeader(null, null, null, null, null);
        offer.AddLine("5 x XYZ-999 Mystery Box", "XYZ-999", null, "Mystery Box", 5, null, null);

        offer.Evaluate(Context(null));

        Assert.True(offer.HasBlockingIssues);
        Assert.Contains(offer.Issues, i => i.Code == IssueCode.UnknownSupplier);
        Assert.Contains(offer.Issues, i => i.Code == IssueCode.MissingCurrency);
        Assert.Contains(offer.Lines[0].Issues, i => i.Code == IssueCode.UnmatchedProduct);
        Assert.Contains(offer.Lines[0].Issues, i => i.Code == IssueCode.MissingUnitPrice);
        Assert.Null(offer.Lines[0].LineTotal);
        Assert.Equal(1, offer.UnpricedLines);
        Assert.Throws<InvalidOperationException>(() => offer.MarkReviewed(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void A_price_well_above_the_last_purchase_is_flagged_but_does_not_block()
    {
        var northgate = Seed.Supplier("NORTHGATE");
        var offer = DraftFor(northgate);
        offer.SetHeader(null, "EUR", Today.AddDays(7), null, null);
        offer.AddLine("line", "NG-114422", null, "Samsung 990 PRO 1TB", 10, 130.00m, null); // last purchase 96.00

        offer.Evaluate(Context(northgate));

        var issue = Assert.Single(offer.Lines[0].Issues);
        Assert.Equal(IssueCode.PriceDeviation, issue.Code);
        Assert.Equal(IssueSeverity.Warning, issue.Severity);
        Assert.False(offer.HasBlockingIssues);
    }

    [Fact]
    public void A_line_total_read_as_a_unit_price_blocks_the_review()
    {
        var adria = Seed.Supplier("ADRIA");
        var offer = DraftFor(adria);
        offer.SetHeader(null, "EUR", Today.AddDays(7), null, null);
        offer.AddLine("AK-70012 HDMI 2.1 cable 2m pack of 10   25 pak   612.50", "AK-70012", null, "HDMI cable", 25, 612.50m, "pak"); // last purchase 24.50 per pack

        offer.Evaluate(Context(adria));

        var issue = Assert.Single(offer.Lines[0].Issues);
        Assert.Equal(IssueCode.PriceDeviation, issue.Code);
        Assert.Equal(IssueSeverity.Blocking, issue.Severity);
        Assert.True(offer.HasBlockingIssues);
    }

    [Fact]
    public void A_correction_by_the_reviewer_recomputes_the_line_and_clears_the_issue()
    {
        var northgate = Seed.Supplier("NORTHGATE");
        var offer = DraftFor(northgate);
        offer.SetHeader(null, "EUR", Today.AddDays(7), null, null);
        var line = offer.AddLine("line", "NG-999999", null, "Something", 4, null, null);
        offer.Evaluate(Context(northgate));
        Assert.True(offer.HasBlockingIssues);

        offer.CorrectLine(line.Id, quantity: null, unitPrice: 58.90m, productId: Seed.Product("SSD-WD-SN580-1T").Id);
        offer.Evaluate(Context(northgate));

        Assert.False(offer.HasBlockingIssues);
        Assert.Equal(235.60m, offer.Lines[0].LineTotal);
        Assert.True(offer.Lines[0].CorrectedByReviewer);
        Assert.Equal(Seed.Product("SSD-WD-SN580-1T").Id, offer.Lines[0].ProductId);

        offer.MarkReviewed(DateTimeOffset.UtcNow);
        Assert.Equal(OfferStatus.Reviewed, offer.Status);
        Assert.Throws<InvalidOperationException>(() => offer.CorrectLine(line.Id, 1, null, null));
    }

    [Fact]
    public void An_expired_offer_blocks_and_a_missing_validity_only_warns()
    {
        var northgate = Seed.Supplier("NORTHGATE");

        var expired = DraftFor(northgate);
        expired.SetHeader(null, "EUR", Today.AddDays(-1), null, null);
        expired.AddLine("line", "NG-114422", null, null, 1, 90m, null);
        expired.Evaluate(Context(northgate));
        Assert.Contains(expired.Issues, i => i.Code == IssueCode.OfferExpired && i.Severity == IssueSeverity.Blocking);

        var open = DraftFor(northgate);
        open.SetHeader(null, "EUR", null, null, null);
        open.AddLine("line", "NG-114422", null, null, 1, 90m, null);
        open.Evaluate(Context(northgate));
        Assert.Contains(open.Issues, i => i.Code == IssueCode.MissingValidity && i.Severity == IssueSeverity.Warning);
        Assert.False(open.HasBlockingIssues);
    }

    [Theory]
    [InlineData(0, ApprovalLevel.None)]
    [InlineData(999.99, ApprovalLevel.None)]
    [InlineData(1000, ApprovalLevel.Manager)]
    [InlineData(9999.99, ApprovalLevel.Manager)]
    [InlineData(10000, ApprovalLevel.Director)]
    public void Approval_levels_follow_the_thresholds(decimal total, ApprovalLevel expected)
    {
        Assert.Equal(expected, ApprovalRules.RequiredFor(total));
    }

    [Fact]
    public void The_same_email_with_different_whitespace_and_case_has_the_same_hash()
    {
        var a = SourceHash.Of("Offer\r\n  20 x SSD  at 97.50 EUR");
        var b = SourceHash.Of("offer 20 x ssd at 97.50 eur");
        var c = SourceHash.Of("offer 21 x ssd at 97.50 eur");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
