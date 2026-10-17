# A Supplier Email Becomes a Draft Offer

*LLM Inside the Monolith, Level 1. Draft for review, 2026-10-17. Every number below comes from `docs/evals/` and from the run recorded in `docs/level-1/report.md`.*

A supplier sends a quote by email. Somebody in purchasing retypes it into the ERP, works out which catalog item each line is, checks the price against the last one, and only then can compare it with the next quote. Hours per day, and the expensive mistakes live exactly there: the wrong item, a mistyped price, a validity date nobody noticed.

This is the first level of a system I am building in public: the back office of a computer hardware reseller, one .NET modular monolith, PostgreSQL, a local model. The rule that holds on every level is set here: the model reads and proposes, the code checks, computes and decides, a person confirms. The model is allowed to be wrong. The system is not allowed to let the error through.

## What you can run

```
git clone <repo>
dotnet run --project DotnetLeet.Trade.AppHost
```

Aspire starts PostgreSQL, Ollama with `llama3.2`, the API and a Blazor review page. Paste a supplier email, and 30 to 40 seconds later (CPU, no GPU) you have a typed draft: supplier, currency, validity, terms, and one line per product with the catalog match, our line total and a list of what is wrong, in red when it blocks the review.

[Screenshot: 03-offer-review-good. A Northgate quote with four lines, all matched by supplier code, 8,387.00 EUR, no issues.]

## Where the LLM lives

An LLM in a solution is not one thing. It is four, and each has a different owner.

| Piece | What it is | Where it lives |
|---|---|---|
| The instructions | The rules for reading an email, in prose, versioned | Application, an embedded resource `offer-extraction-v2.md` |
| The email | Input data | Crosses the boundary like any other command |
| The draft | Nullable fields, strings as the supplier wrote them | `OfferExtraction`, owned by Application |
| The model | Provider, endpoint, temperature, JSON schema | Infrastructure, behind one port |

The port does not say "AI". A person with a form could implement it.

```csharp
public interface IOfferExtractor
{
    Task<OfferExtraction> ExtractAsync(string emailText, CancellationToken ct = default);
}

public sealed record ExtractedLine(
    string RawText,
    string? SupplierCode,
    string? ManufacturerPartNumber,
    string? Description,
    decimal? Quantity,
    decimal? UnitPrice,
    string? Unit);
```

The nullable numbers are the design. What the model could not read stays null, and null becomes an issue a person sees. Nothing in the pipeline fills a gap with a guess.

The domain never hears about the model. It gets a `SupplierOffer` with lines, matches every line to the catalog by code (our SKU, the supplier's article code, the manufacturer part number, EAN, and as a last resort a known code found inside the raw line text), computes line totals and the subtotal, and runs the rules. A test fails the build if the Domain or Application project ever references an AI or database package.

## The bug that produced an empty answer

The first eval run scored 66%, and the reason was not the model. Some emails came back as `{}`. `Microsoft.Extensions.AI` builds the JSON schema from the DTO, and that schema had no `required` list. With the schema as a grammar, an empty object is valid, so a 3B model sometimes took the shortest valid path.

```csharp
private static readonly ChatResponseFormat ResponseFormat = ChatResponseFormat.ForJsonSchema(
    AIJsonUtilities.CreateJsonSchema(
        typeof(OfferCompletion),
        inferenceOptions: new AIJsonSchemaCreateOptions
        {
            TransformOptions = new AIJsonSchemaTransformOptions
            {
                RequireAllProperties = true,
                DisallowAdditionalProperties = true
            }
        }),
    schemaName: "SupplierOffer");
```

Every property required, nullable ones included. The empty answers stopped. 66% became 85%.

## Schema valid is not correct

The second example is an email from a Bosnian supplier, cables sold per pack, and one line that only states a total: 25 packs, 612.50 in total. The model wrote 612.50 as the unit price. Valid JSON, valid schema, and a line worth 15,312.50 instead of 612.50.

[Screenshot: 04-offer-review-problems. The HDMI line with the blocking message: 25 times the last purchase price of 24.50 EUR.]

The domain caught it, not the model and not the prompt. We bought those packs six weeks ago at 24.50, and a unit price three times the last purchase price blocks the review:

```csharp
if (price >= last.UnitPrice * GrossDeviationFactor)
    line.AddIssue(OfferIssue.Blocking(IssueCode.PriceDeviation,
        $"{ratio} times the last purchase price of {lastPrice} {last.Currency}. A line total or a misplaced decimal point, check the email."));
else if (price > last.UnitPrice * (1 + PriceDeviationThreshold))
    line.AddIssue(OfferIssue.Warning(IssueCode.PriceDeviation, ...));
```

The reviewer reads the email, types 24.50, the rules run again, the line total becomes 612.50, and confirm passes. Before the correction, confirm returns 409 with the domain's sentence: an offer with blocking issues cannot be marked as reviewed.

[Screenshot: 06-offer-review-confirmed.]

Two more things the use case guarantees. The same email twice is the same offer: the text is hashed, and a second submission returns the existing draft without a second model call. And every model call is a row in `ai.calls`: the input, the raw answer, the instructions version, tokens, duration. The input to the model is the specification of the feature. Keep it.

[Screenshot: 05-ai-calls.]

## Evidence, not a feeling

Twelve supplier emails, written to cover what real quotes do: supplier codes, part numbers only, GBP and USD, thousands separators, packs, quantity tiers, no currency, an expired offer, a prompt injection, an unknown sender, a forwarded thread full of noise. For each, the expected values were written by a person before the model saw the email. Every field is one check, and the test runner is the report.

| Run | Change | Fields correct | Emails fully correct |
|---|---|---|---|
| 1 | instructions v1, schema as generated | 161/243 (66%) | 2/12 |
| 2 | every property required | 206/243 (85%) | 2/12 |
| 3 | matcher finds a known code in the line text | 219/243 (90%) | 4/12 |
| 4 | instructions v2: dates as yyyy-MM-dd, currency symbols, "ships in 24 hours" is 0 days | 224/243 (92%) | 5/12 |

What still fails with llama3.2: a currency stated once in a sentence is missed in half the emails, "EUR 168.40" loses its decimal point, a date written in words is sometimes left empty, and the division above. None of it reaches the business. A missing currency and an unmatched line are blocking issues. The reviewer sees red and cannot confirm.

[Screenshot: 09-aspire-trace-detail. One extraction as an Aspire trace: 36.9 s end to end, 36 s of it in the chat span to Ollama, six PostgreSQL calls under 40 ms each.]

## Limitations

- A 3B model on a CPU: 20 to 40 seconds per email. A cloud model would be faster and more accurate, and Level 6 measures exactly that on the same twelve emails.
- The deviation rules need purchase history. A product we never bought has no yardstick.
- Matching is codes only. A line with a description and no code stays unmatched, by design.
- No PDF attachments, no reply to the supplier, no purchase order. An offer never changes stock and never orders anything.

## The rule

**The model may be wrong. The system may not let the error through.** Level 1 is where that split is built: instructions in Application, the schema next to the adapter, rules and arithmetic in the domain, a person at the end. Everything that comes later, memory, documents, tools, an agent, sits on top of this and never replaces it.
