# Eval history

Twelve supplier emails, field-level checks written by a person before the model saw them.
Model: llama3.2 (3B) on CPU through Ollama, temperature 0. Each file is one run of
`TRADE_EVALS=1 dotnet test DotnetLeet.Trade.Evals`.

## Level 1: email to draft offer

| Run | Change | Fields correct | Cases fully correct |
|---|---|---|---|
| 1 | instructions v1, JSON schema as Microsoft.Extensions.AI generates it | 161/243 (66%) | 2/12 |
| 2 | schema with every property required (no more empty `{}` answers) | 206/243 (85%) | 2/12 |
| 3 | matcher also finds a known code inside the raw line text | 219/243 (90%) | 4/12 |
| 4 | instructions v2 (dates as yyyy-MM-dd, currency symbols, 24 hours is 0 days, tiers carry the product) | 224/243 (92%) | 5/12 |

What still fails after run 4, and why it is not fixed at this level:

- Currency stated once in a sentence ("prices in EUR excl. VAT") is missed in 6 of 12 emails. The model limit, not the instructions: run 3 with shorter wording missed it in 3.
- "EUR 168.40" becomes 1684 (the decimal point is dropped) in one email. A 3B model reading a currency code before the number.
- A line with only a line total gets a unit price the model computed by division, although the instructions forbid it.
- A validity date written in words ("6 November 2026", "20.10.2026.") is sometimes left empty.

None of these reach the business: an empty currency and an unmatched line are blocking issues, a reviewer sees them in red and cannot confirm the offer until they are fixed. Level 6 compares this model against a cloud model on the same twelve emails.
