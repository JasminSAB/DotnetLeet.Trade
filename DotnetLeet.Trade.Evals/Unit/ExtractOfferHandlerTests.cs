using DotnetLeet.Trade.Application.Procurement.ExtractOffer;
using DotnetLeet.Trade.Application.Procurement.Instructions;
using DotnetLeet.Trade.Application.Procurement.ReviewOffer;
using DotnetLeet.Trade.Domain.Procurement;
using DotnetLeet.Trade.Evals.Fakes;
using Microsoft.Extensions.AI;

namespace DotnetLeet.Trade.Evals.Unit;

/// <summary>Our code with a canned model. No network, no tokens, runs on every build.</summary>
public sealed class ExtractOfferHandlerTests
{
    private const string Email = """
        From: quotes@northgate-dist.example
        Subject: Quote Q-26-4471

        Hello,

        as requested, our offer:

        20 pcs  NG-114422  Samsung 990 PRO 1TB NVMe   97.50 EUR
        10 pcs  NG-131002  Kingston FURY Beast 32GB kit  86.00 EUR
         4 pcs  NG-777777  Mystery cable kit            12.00 EUR

        Prices per piece, EUR, excl. VAT. Valid until 2026-10-31. Payment 30 days net. Delivery 3 days.

        Best regards,
        Northgate Distribution GmbH
        """;

    private const string Answer = """
        {
          "supplierName": "Northgate Distribution GmbH",
          "supplierReference": "Q-26-4471",
          "currency": "EUR",
          "validUntil": "2026-10-31",
          "paymentTermsDays": 30,
          "deliveryDays": 3,
          "lines": [
            { "rawText": "20 pcs  NG-114422  Samsung 990 PRO 1TB NVMe   97.50 EUR", "supplierCode": "NG-114422", "manufacturerPartNumber": null, "description": "Samsung 990 PRO 1TB NVMe", "quantity": 20, "unitPrice": 97.50, "unit": "pcs" },
            { "rawText": "10 pcs  NG-131002  Kingston FURY Beast 32GB kit  86.00 EUR", "supplierCode": "NG-131002", "manufacturerPartNumber": null, "description": "Kingston FURY Beast 32GB kit", "quantity": 10, "unitPrice": 86.00, "unit": "pcs" },
            { "rawText": " 4 pcs  NG-777777  Mystery cable kit            12.00 EUR", "supplierCode": "NG-777777", "manufacturerPartNumber": null, "description": "Mystery cable kit", "quantity": 4, "unitPrice": 12.00, "unit": "pcs" }
          ],
          "unknowns": []
        }
        """;

    [Fact]
    public async Task An_email_becomes_a_draft_offer_with_matched_lines_and_our_totals()
    {
        var model = new CannedChatClient(Answer);
        var host = new TradeHost(model);

        var result = await host.Extract.HandleAsync(new ExtractOfferFromEmailCommand(Email, "quotes@northgate-dist.example"));
        var offer = (await host.Queries.GetAsync(result.OfferId))!;

        Assert.False(result.AlreadyExisted);
        Assert.Equal("Northgate Distribution GmbH", offer.SupplierName);
        Assert.Equal("EUR", offer.Currency);
        Assert.Equal(new DateOnly(2026, 10, 31), offer.ValidUntil);
        Assert.Equal(3, offer.Lines.Count);

        Assert.Equal("SSD-SAM-990P-1T", offer.Lines[0].ProductSku);
        Assert.Equal(1950.00m, offer.Lines[0].LineTotal);
        Assert.Equal("RAM-KIN-FB-32K-5600", offer.Lines[1].ProductSku);
        Assert.Null(offer.Lines[2].ProductSku);
        Assert.Contains(offer.Lines[2].Issues, i => i.Code == IssueCode.UnmatchedProduct);

        Assert.Equal(1950.00m + 860.00m + 48.00m, offer.Subtotal);
        Assert.Equal(ApprovalLevel.Manager, offer.ApprovalIfOrdered);
        Assert.Equal(OfferStatus.Draft, offer.Status);

        var call = Assert.Single(host.Store.AiCalls);
        Assert.Equal(OfferExtractionInstructions.Version, call.InstructionsVersion);
        Assert.Equal(Email, call.Input);
        Assert.Equal(ChatRole.System, model.LastMessages[0].Role);
        Assert.Equal(OfferExtractionInstructions.Text, model.LastMessages[0].Text);
    }

    [Fact]
    public async Task The_same_email_twice_does_not_call_the_model_again()
    {
        var model = new CannedChatClient(Answer);
        var host = new TradeHost(model);

        var first = await host.Extract.HandleAsync(new ExtractOfferFromEmailCommand(Email, "quotes@northgate-dist.example"));
        var second = await host.Extract.HandleAsync(new ExtractOfferFromEmailCommand("  " + Email.ToUpperInvariant() + "\n", "quotes@northgate-dist.example"));

        Assert.Equal(first.OfferId, second.OfferId);
        Assert.True(second.AlreadyExisted);
        Assert.Equal(1, model.Calls);
        Assert.Single(host.Store.Offers);
    }

    [Fact]
    public async Task A_reviewer_fixes_the_unmatched_line_and_confirms()
    {
        var host = new TradeHost(new CannedChatClient(Answer));
        var result = await host.Extract.HandleAsync(new ExtractOfferFromEmailCommand(Email, "quotes@northgate-dist.example"));
        var draft = (await host.Queries.GetAsync(result.OfferId))!;

        await Assert.ThrowsAsync<InvalidOperationException>(() => host.Review.ConfirmAsync(new ConfirmOfferReviewCommand(result.OfferId)));

        var cable = host.Store.Seed.Product("CAB-CAT6-2M-10");
        await host.Review.CorrectLineAsync(new CorrectOfferLineCommand(result.OfferId, draft.Lines[2].Id, null, null, cable.Id));
        await host.Review.ConfirmAsync(new ConfirmOfferReviewCommand(result.OfferId));

        var reviewed = (await host.Queries.GetAsync(result.OfferId))!;
        Assert.Equal(OfferStatus.Reviewed, reviewed.Status);
        Assert.Equal("CAB-CAT6-2M-10", reviewed.Lines[2].ProductSku);
        Assert.True(reviewed.Lines[2].CorrectedByReviewer);
        Assert.Empty(reviewed.Lines[2].Issues.Where(i => i.Severity == IssueSeverity.Blocking));
    }

    [Fact]
    public async Task An_unknown_sender_leaves_the_supplier_open_for_the_reviewer()
    {
        var host = new TradeHost(new CannedChatClient(Answer.Replace("Northgate Distribution GmbH", "Someone Else Ltd")));

        var result = await host.Extract.HandleAsync(new ExtractOfferFromEmailCommand(Email, "sales@unknown.example"));
        var offer = (await host.Queries.GetAsync(result.OfferId))!;

        Assert.Null(offer.SupplierId);
        Assert.Contains(offer.Issues, i => i.Code == IssueCode.UnknownSupplier);
        Assert.All(offer.Lines, l => Assert.Null(l.ProductSku)); // supplier codes cannot match without a supplier
    }

    [Fact]
    public async Task A_blank_email_never_reaches_the_model()
    {
        var model = new CannedChatClient(Answer);
        var host = new TradeHost(model);

        await Assert.ThrowsAsync<ArgumentException>(() => host.Extract.HandleAsync(new ExtractOfferFromEmailCommand("   ", null)));

        Assert.Equal(0, model.Calls);
    }
}
