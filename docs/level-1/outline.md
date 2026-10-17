# Post 1 outline: A Supplier Email Becomes a Draft Offer

## 1. Opening: the system, the problem, the reason for a model (before any command)

- DotnetLeet.Trade is the back office of a computer hardware reseller, built in public one working slice at a time. This slice is purchasing.
- The problem now: a supplier's email becomes a checked draft offer in the system, instead of being retyped by hand.
- Why a model: offers arrive as prose, tables made of spaces, pipes, tiers, in four currencies and three number formats. Parsers exist and every format adds maintenance.
- Why this example: it mixes messy text with exact business numbers, so it shows the difference between a valid JSON answer and correct data.
- What we are exploring: how an unreliable output fits into an application that has business rules, validation and a person at the end.
- What the reader can run and learn by the end: run the app, paste an email, see the draft and the issues, understand where each piece of the model lives, and read a measured result with its limits.
- Suppliers, emails, catalog and purchase history are synthetic demo data, said once in the intro.

## 2. The two examples that run through the post

- Northgate: a plain list with supplier codes, 4 lines, all matched, 8,387.00 EUR, no issues, confirmed.
- Adria Komponente: cables per pack, one line states only a total, the model writes the total as the unit price, the deviation rule blocks the line, a person corrects it, confirm passes.

## 3. Run it

Prerequisites (Docker Desktop, .NET 10 SDK), clone, cd, tag `level-1`, one command, what Aspire starts, what the first start does, where the two example emails are in the repo, how long a call takes on CPU.

## 4. Where the model lives (architecture decisions, one claim each)

- Four pieces, four owners: instructions, email, draft, model.
- The port does not say AI; nullable fields are the design. Null can express unknown, it does not stop the model from inventing a value (shown in section 6).
- The domain never hears about the model: matching, totals, rules.
- Idempotency by content hash; every model call is a row in `ai.calls`.

## 5. Schema valid is not correct

- The `required` fix: empty answers stopped in the measured run, score moves from run 1 to run 2 in the table. No explanation of why the model produced them.
- The Adria line: valid JSON, wrong number, blocked by the deviation rule because a purchase price existed.
- What the rules catch and what they do not: plausible wrong values pass, hence the reviewer.

## 6. Measuring on twelve emails, then five held-out

- What the set is, how a check works, the five runs on one basis (231 checks, one checker), pointer to the reconstruction in `docs/evals/README.md`.
- The split: model fields versus catalog matching, so a pipeline change is not credited to the model.
- The honest reading: a development set, tuned and measured on the same twelve. Final state 215/231 fields, 5/12 emails fully correct.
- Held-out: five emails, frozen system, one run, 78/89, 0/5. Invented date and payment term, the rules cannot see that.

## 7. Limitations

Latency on CPU, 3B model limits, codes-only matching, deviation rules need history, no PDF, no order, internal UI, and no measurement yet on unseen offers or other models.

## 8. Closing: what this level showed

Shown: a reviewable flow of extraction, validation, correction and save, with every model call recorded. Not shown: that this model and configuration can be trusted without the person; no time saving measured; nothing supports autonomous processing. Then the contract: the model drafts, the rules catch what they are written to catch, a person decides.

## Screenshots

01 offers list, 03 Northgate reviewed, 04 Adria with the blocking line, 06 Adria corrected and confirmed, 05 AI calls, 09 Aspire trace of one extraction.
