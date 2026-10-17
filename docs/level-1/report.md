# Level 1 report: what works, what is tested, what was missing

Date: 2026-10-17. Scope: supplier email to draft offer, validation, manual correction, save.

## What works (verified against the running app under Aspire)

| Step | Where | Verified how |
|---|---|---|
| Email in | `POST /procurement/offers/from-email`, Blazor page `/offers/new` | API call with eval case 01 and 05, HTTP 201 in 28 to 37 s on CPU |
| Model extracts a draft | `ModelOfferExtractor` (Infrastructure), instructions `offer-extraction-v2` (Application), JSON schema with every property required | raw answer stored in `ai.calls`, visible on `/ai-calls` |
| Lines matched to the catalog | `ProductMatcher` (Domain): SKU, supplier code, part number, EAN, then a known code inside the raw line | case 01: 4 of 4 lines matched by supplier code; case 05: 4 of 4 |
| Totals and rules | `SupplierOffer.Evaluate` + `OfferRules` (Domain): line totals, subtotal, unknown supplier, missing currency, missing or expired validity, unmatched product, missing quantity or price, duplicate line, unit mismatch, price deviation (warning above 15%, blocking at 3x) | unit tests, and the live case 05 run |
| Same email twice | `SourceHash` in the use case, unique index on the hash | second POST returned 200 with `alreadyExisted = true` and the same id, no second model call |
| Manual correction | `PUT /procurement/offers/{id}/lines/{lineId}`, `PUT .../header`, Blazor review page | live: set currency EUR, set a unit price, pick a product; every correction re-runs the rules |
| Confirm or reject | `POST .../confirm` (409 with the reason while blocking issues remain), `POST .../reject` | live: 409 before the correction, 204 after, status Reviewed with a timestamp |
| Persistence | PostgreSQL, schemas `catalog`, `procurement`, `inventory`, `ai`, EF Core migration `Level1` | migrate and seed on first start, 42 products, 5 suppliers, 36 purchase prices |

## What is tested without the model (30 tests, run on every build)

- Architecture: Domain references no AI or database package and no other layer; Application references only Domain; only Infrastructure references `Microsoft.Extensions.AI` and EF Core; the Web project does not reference Infrastructure or Domain.
- Domain: matching by every code kind, no cross-supplier code match, code found inside the line text, totals, blocking and warning rules, gross price deviation, expired versus missing validity, approval thresholds, source hash normalisation.
- Use case with a canned model: email to draft with matched lines and our totals, idempotency, reviewer correction and confirm, unknown sender, blank email never reaches the model, the adapter sends a schema with `required` arrays.
- The eval set itself: 12 cases load and reference real catalog data.

## What is measured with the model (12 emails, field checks, llama3.2 on CPU)

66% (run 1) to 85% (required schema) to 90% (code in line) to 92% (instructions v2). Details in `docs/evals/`.

## What was missing for the demonstration, and was added today

1. The API to model HTTP client inherited the standard resilience handler from ServiceDefaults: 10 s per attempt, retries, 30 s total. Every real extraction was cut off. Fixed with `ConfigureAll<HttpStandardResilienceOptions>` in the API: 3 min attempt, 5 min total, no retry (Polly rejects `MaxRetryAttempts = 0`, so one attempt with a predicate that never retries).
2. A line total read by the model as a unit price (612.50 for a pack that costs 24.50) passed with only a unit warning, because the HDMI cable had no purchase history and "pak" was not a known pack word. Added the gross deviation rule (3x the last purchase blocks), the Bosnian pack words, and the missing purchase price in the seed.
3. `confirm` returned a generic 500 when the domain refused. Now 409 with the domain's message, so the UI can show it.

## Known limitations (stay in the post)

- llama3.2 on CPU: 20 to 40 s per email, and the eval failures listed in `docs/evals/README.md` (currency in a sentence, decimal point after a currency code, division for unit price, dates in words).
- The price deviation rules only work for products we bought before. A first purchase has no yardstick.
- Matching is codes only. A line with a description and no code stays unmatched by design.
- No PDF, no attachments, no reply to the supplier, no purchase order. Later levels.
- The Blazor review page is an internal tool, not a product UI.
