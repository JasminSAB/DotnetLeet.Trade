# Eval history

Model: llama3.2 (3B) on CPU through Ollama, temperature 0. Field-level checks written by a
person before the model saw the email. One run is `TRADE_EVALS=1 dotnet test DotnetLeet.Trade.Evals`.

Two sets:

- `cases/`: 12 emails, the development set. Every change to the system was made after reading
  these twelve and measured on the same twelve.
- `holdout/`: 5 emails written after the instructions (v3), the rules and the checker were
  frozen. Run once, never used to change anything. If the system changes after that run, these
  five join the development set and a new held-out set is needed.

## Level 1: email to draft offer, development set

The checks split into two kinds. Fields the model fills in (header fields, quantity and unit
price per line) are the model's work. Catalog matches per line (41 of them) are done by
deterministic code from the model's output. A better matcher is not a better model.

The checker changed once: the check for numbers that do not appear in the email joined digits
across whitespace in runs 1 and 2, and was fixed before run 3. The raw model answers of runs 1
and 2 were not kept, so the runs cannot be re-scored by running the checker again.

How the comparison below was reconstructed: each per-run report (`level1-run1*.md` to
`level1-run5*.md`) lists every failed check per email with the expected and the actual value.
Starting from the 243 checks of the full checker, the one check that changed (numbers not in
the email) was removed from every run, which leaves 231 checks. For each run the failures in
its report were then counted again against those 231 checks, with the delivery field of email
04 scored against the corrected ground truth (availability is not a delivery time). The split
into model fields (190) and catalog matching (41) follows the same failure lists. The per-run
report files keep the numbers as they were produced at the time; only the table below is the
comparable view. Runs 3 to 5 have their raw answers in `evals-raw/` under the test output
folder, so for those the reconstruction can be verified by re-running the checker.

| Run | Change | What it changed | All fields | Model fields | Matching | Emails fully correct |
|---|---|---|---|---|---|---|
| 1 | instructions v1, JSON schema as Microsoft.Extensions.AI generates it | baseline | 155/231 (67%) | 132/190 (69%) | 23/41 | 1/12 |
| 2 | schema with every property required, so `{}` is no longer a valid answer | model input | 200/231 (87%) | 169/190 (89%) | 31/41 | 2/12 |
| 3 | matcher also finds a known code inside the raw line text | pipeline, not the model | 208/231 (90%) | 169/190 (89%) | 39/41 | 4/12 |
| 4 | instructions v2: dates as yyyy-MM-dd, currency from symbols and per-line codes, numbers digit for digit, tier lines repeat the product | model input | 212/231 (92%) | 172/190 (91%) | 40/41 | 4/12 |
| 5 | instructions v3: availability ("in stock", "ships within 24 hours") is not a delivery time and leaves the field empty | model input | 215/231 (93%) | 175/190 (92%) | 40/41 | 5/12 |

Run 3 changed nothing the model sees, and its model column is identical to run 2. The final
development state is run 5: 226 of 243 checks with the full checker, 5 of 12 emails fully
correct.

What still fails after run 5:

- A currency stated once in a sentence ("prices in EUR excl. VAT") is missed in 5 of 12 emails.
- "EUR 168.40" becomes 1684 in one email: the decimal point is dropped when a currency code
  comes before the number.
- A line with only a line total gets a unit price the model computed by division, although
  the instructions forbid it. In the live run the deviation rule blocked it because the product
  had a purchase history; without one it would have passed with a unit warning only.
- A validity date written in words is sometimes left empty.

The rules catch specific classes of error: missing fields, unknown codes, prices far from the
last purchase, expired offers. A wrong value that looks plausible passes them. That is why a
person reviews every draft before it is confirmed.

## Held-out set

See `level1-holdout.md` for the one run on the five unseen emails.
