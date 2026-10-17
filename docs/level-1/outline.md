# Post 1 outline: A Supplier Email Becomes a Draft Offer (LLM Inside the Monolith, Level 1)

## Who it is for, and the problem

.NET developers in companies that buy things: resellers, distributors, any back office with a purchasing desk. The job today: a supplier emails a quote, a person retypes it into the ERP, looks up which catalog item each line is, checks the price against the last one, and only then can compare offers. Hours per day, and the expensive mistakes live there: wrong item, mistyped price, missed validity date.

The claim: the model may read the email, but it must never decide anything. Code matches, computes and blocks. A person confirms.

## What the reader can run and learn

- Clone, `dotnet run --project DotnetLeet.Trade.AppHost`, paste an email, watch it become a typed draft with issues in red. Aspire brings PostgreSQL, Ollama and the model.
- Where each piece of the LLM lives in a clean architecture solution: instructions as a versioned embedded resource in Application, the schema DTO next to the adapter in Infrastructure, the domain that never hears about AI.
- Why "schema valid" is not "correct", and what catches the difference.
- How to measure instead of trusting: 12 emails, field checks, four runs, 66% to 92%.

## Two examples from the real run

1. Northgate, a plain list with supplier codes: 4 lines, all matched, 8,387.00 EUR, Manager approval if ordered, no issues, confirmed in one click. 36.7 s on CPU.
2. Adria Komponente, cables in packs with one line that only states a total: the model wrote the total 612.50 as the unit price. The domain blocked the line (25 times the last purchase price), currency was missing, the reviewer set EUR and 24.50, the rules re-ran, confirm passed.

## Architecture decisions (short, one claim each)

- The LLM is four things in four places: instructions, input, output contract, model.
- The port does not say "AI": `IOfferExtractor`, email in, draft out.
- Nullable numbers are the point: unknown stays null and becomes an issue.
- The model reads, the code computes: line totals, subtotal, deviation against purchase history.
- Idempotent by content: the same email is the same offer.
- The audit trail is a table, not a log line: `ai.calls` with input, raw output, instructions version, tokens, duration.

## Code to show (small, real)

- `IOfferExtractor` and `OfferExtraction` (the port).
- The `required` schema fix in `ModelOfferExtractor` (the bug that produced `{}`).
- `OfferRules` excerpt: missing price, unmatched product, gross deviation.
- `ExtractOfferFromEmailHandler` (the use case, no retries, no prompt text).
- One eval case JSON and the field check idea.

## Results (measured, from `docs/evals/`)

| Run | Change | Fields | Cases fully correct |
|---|---|---|---|
| 1 | v1, default schema | 66% | 2/12 |
| 2 | every property required | 85% | 2/12 |
| 3 | code found in line text | 90% | 4/12 |
| 4 | instructions v2 | 92% | 5/12 |

What still fails and why it does not reach the business.

## Limitations

CPU latency, 3B model limits, codes-only matching, no PDF, no purchase order, internal UI.

## Screenshots

Offers list, review page with blocking issues, review page after correction, AI calls, Aspire trace of one extraction.

## Closing rule

The model may be wrong. The system may not let the error through. Level 1 is where that split is built, before any agent.
