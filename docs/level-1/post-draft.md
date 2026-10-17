# A Supplier Email Becomes a Draft Offer

*LLM Inside the Monolith, Level 1. Draft 4 for final review, 2026-10-18. Every number comes from `docs/evals/` in the repo.*

DotnetLeet.Trade is the back office of a computer hardware reseller, a company that buys from distributors and sells to businesses. I am building it in public, one working slice at a time, and the first slice is purchasing. A supplier sends a quote by email. Someone retypes it into the system, works out which catalog item each line is, checks the price against what we paid last time, and only then can compare it with the next quote. The expensive mistakes live exactly there: the wrong item, a mistyped price, a validity date nobody noticed.

The emails have no format. One supplier writes a plain list with its own article codes, another quotes by manufacturer part numbers, a third sends a table made of pipes, a fourth gives quantity tiers, and prices arrive in EUR, GBP and USD in three number formats. Hand-written parsers for that exist, and each new format means more parser to maintain. A language model reads all of it, and it makes mistakes of its own. That is what this slice is about: how an unreliable output fits into an application that has business rules, validation and a person at the end. The model reads and proposes. Code matches, computes and checks what it can check. A person reviews and confirms.

By the end you can run the system on your machine, paste a supplier email, see the draft with its problems listed, correct one and confirm it. You will see where each piece of the model lives in a clean architecture solution, why a valid JSON answer is not the same as correct data, and measured results with their limits. Two emails run through the post: a plain quote from a German distributor, Northgate, and one from a Bosnian supplier, Adria Komponente, which goes wrong in an instructive way. The suppliers, the emails, the catalog and the purchase history are synthetic demo data written for this series. No real company or transaction appears in it.

## Run it

You need Docker Desktop and the .NET 10 SDK. Everything else comes from the Aspire AppHost.

```
git clone https://github.com/JasminSAB/DotnetLeet.Trade.git
cd DotnetLeet.Trade
git checkout level-1
dotnet run --project DotnetLeet.Trade.AppHost
```

Aspire starts PostgreSQL, Ollama with `llama3.2`, the API and a Blazor review page, and seeds a catalog of 42 products, 5 suppliers and their last purchase prices. The first start downloads the model, about 2 GB, into a Docker volume. The two emails from this post are in the repo as eval cases, the `email` field of `DotnetLeet.Trade.Evals/Evals/cases/01-northgate-plain-list.json` and `05-adria-packs-line-total.json`. Paste the Northgate one on the "New offer" page. On a CPU the model needs 30 to 40 seconds. Then you get this:

[Screenshot 03: Northgate, four lines, every one matched by supplier code, line totals and the subtotal of 8,387.00 EUR, no issues, confirmed.]

## Where the model lives

A language model in a solution is not one thing. It is four, and each has a different owner.

| Piece | What it is | Where it lives |
|---|---|---|
| The instructions | How to read a supplier email, in prose, versioned | Application, an embedded resource `offer-extraction-v3.md` |
| The email | Input data | Crosses the boundary like any other command |
| The draft | Nullable fields, strings as the supplier wrote them | `OfferExtraction`, a record owned by Application |
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

The nullable numbers are the design. What the model could not read stays null, and null becomes an issue a person sees. That gives the draft a way to say "unknown". It does not stop the model from writing a value that is not in the email, and the last section shows that happening.

The domain never hears about the model. It receives a `SupplierOffer` with lines, matches each line to the catalog by code (our SKU, the supplier's article code, the manufacturer part number, EAN, or a known code found inside the raw line text), computes line totals and the subtotal, and runs the rules: unknown supplier, missing currency, expired validity, unmatched product, missing quantity or price, price far from the last purchase. A test fails the build if Domain or Application references an AI or database package.

Two more guarantees sit in the use case. The same email twice is the same offer: the text is hashed, and a second submission returns the existing draft without a second model call. And every model call is a row in `ai.calls`: input, raw answer, instructions version, tokens, duration. The input to the model is the specification of the feature. Keep it.

[Screenshot 05: the AI calls page.]

## Schema valid is not correct

In the first eval run the biggest loss was not the model's reading. Some emails came back as `{}`. `Microsoft.Extensions.AI` builds the JSON schema from the DTO, and that schema had no `required` list. With the schema used as a grammar, an empty object is a valid answer.

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

Every property required, nullable ones included. In the next run no email came back empty, and the score moved from run 1 to run 2 in the table below. I did not look into why the model produced empty objects, only that they stopped after the schema change in the run I measured.

Now the Adria email. Cables per pack, and one line states only a total: 25 packs, 612.50 in total. The model wrote 612.50 as the unit price. Valid JSON, every field present, and a line worth 15,312.50 instead of 612.50.

[Screenshot 04: the HDMI line with the blocking message, 25 times the last purchase price of 24.50 EUR.]

The domain caught it. The seeded purchase history has those packs bought six weeks earlier at 24.50, and a unit price three times the last purchase price blocks the review:

```csharp
if (price >= last.UnitPrice * GrossDeviationFactor)
    line.AddIssue(OfferIssue.Blocking(IssueCode.PriceDeviation,
        $"{ratio} times the last purchase price of {lastPrice} {last.Currency}. A line total or a misplaced decimal point, check the email."));
else if (price > last.UnitPrice * (1 + PriceDeviationThreshold))
    line.AddIssue(OfferIssue.Warning(IssueCode.PriceDeviation, ...));
```

The reviewer reads the email, types 24.50, the rules run again, the line total becomes 612.50, and confirm passes. Before the correction, confirm returns 409 with the domain's sentence: an offer with blocking issues cannot be marked as reviewed.

[Screenshot 06: Adria corrected and confirmed.]

What the rule did and did not do: it caught a number 25 times the last price because a last price existed. For a product never bought before, the same mistake would have produced a unit warning and nothing else. A wrong value that looks plausible, 26.50 instead of 24.50, passes every rule in this system. The rules catch the classes of error they were written for. The person reviewing the draft is the last check, not a formality.

## Measuring on twelve emails, then on five more

Twelve supplier emails cover what quotes do: supplier codes, part numbers only, GBP and USD, thousands separators, packs, quantity tiers, no currency, an expired offer, a prompt injection, an unknown sender, a forwarded thread. For each, I wrote the expected values before the model saw the email. Every field is one check, every email is one xUnit test.

Most checks are fields the model fills in. 41 are catalog matches, which deterministic code does from the model's output. They are kept apart because a better matcher is not a better model. The table scores all five runs with the same checker over the same 231 checks. How that comparison was put together from the saved reports is in `docs/evals/README.md`.

| Run | Change | All fields | Model fields | Matching | Emails fully correct |
|---|---|---|---|---|---|
| 1 | instructions v1, schema as generated | 155/231 | 132/190 | 23/41 | 1/12 |
| 2 | every property required | 200/231 | 169/190 | 31/41 | 2/12 |
| 3 | matcher finds a known code in the line text | 208/231 | 169/190 | 39/41 | 4/12 |
| 4 | instructions v2: dates as yyyy-MM-dd, currency symbols, numbers digit for digit | 212/231 | 172/190 | 40/41 | 4/12 |
| 5 | instructions v3: availability is not a delivery time | 215/231 | 175/190 | 40/41 | 5/12 |

Run 3 touched nothing the model sees, and its model column is identical to run 2. The whole gain is matching. Run 5 corrects a mistake of mine: v2 told the model that "in stock" and "ships within 24 hours" mean a delivery time of 0 days. Availability, dispatch and delivery are three different things, so v3 leaves the delivery field empty for them, and the expected value was corrected too.

This is a development set. Every change was made after reading these twelve emails and measured on the same twelve. So I froze the instructions, the rules and the checker, wrote five new emails in styles the system had not seen (decimal commas and a German validity date, a quote written as a sentence, part numbers with one line total, an unknown sender quoting our own SKUs, a forwarded thread), and ran them once.

| Set | All fields | Model fields | Matching | Emails fully correct |
|---|---|---|---|---|
| Held-out, 5 emails, one run | 78/89 | 66/76 | 12/13 | 0/5 |

The failures repeat the development set, plus two new ones. The currency in a sentence is missed in three of five. "Ships within 24 hours" still became 24 delivery days despite v3. In three emails the model filled in a validity date that is not in the email, and in one a payment term of 0 days. Those are invented values, exactly what the instructions forbid, and on the review page they look like any other value. The rules have nothing to compare them against, so they pass. Only a reviewer who reads the email next to the draft would notice. Five emails are a small check, not a proof of reliability, and now that I have read their results they are part of the development set.

[Screenshot 09: one extraction as an Aspire trace, 36.9 s end to end, 36 s in the chat span to Ollama, six PostgreSQL calls under 40 ms each.]

## Limitations

- A 3B model on a CPU: 30 to 40 seconds per email on my machine. I have not measured another model or a GPU, so I make no claim about either.
- Twelve development emails and five held-out ones. Small numbers, one model, one run each.
- The model invents values sometimes (a date, a payment term) and the rules cannot see that. The reviewer can, if they look.
- The deviation rules need purchase history. A product never bought before has no yardstick.
- Matching is codes only, and a known code shorter than six characters is not searched for inside the line text.
- No PDF attachments, no reply to the supplier, no purchase order. An offer never changes stock and never orders anything.
- The review page is an internal tool, not a product UI.

## What this level showed

A supplier email can become a typed draft, be checked by rules the domain owns, be corrected by a person and be saved, with every model call recorded and the same email never extracted twice. That flow works and you can run it. What it has not shown is that the result can be trusted without the person. This model and this configuration get most fields right and still invent a date or copy a line total into a unit price, and the rules only catch the part of that they were written for. I have not measured how much time the flow saves against retyping, and nothing here supports letting it run on its own.

**The model drafts. The rules catch what they were written to catch. A person decides.** Level 1 is where that contract is built: instructions in Application, the schema next to the adapter, matching and arithmetic in the domain, a reviewer at the end. Anything added later has to keep it.
