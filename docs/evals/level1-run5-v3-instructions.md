# Offer extraction evals, 2026-10-17 21:27 UTC

Model llama3.2:latest, instructions offer-extraction-v3, 12 cases, 226/243 fields (93%), 5 cases fully correct, 17574 tokens, 311 s.

| Case | Fields | Tokens | Seconds | Failures |
|---|---|---|---|---|
| 01-northgate-plain-list | 22/22 | 1498 | 25.4 |  |
| 02-baltic-mpn-only | 16/22 | 1578 | 32.2 | currency expected 'EUR' got ''; line1.unitPrice expected '168.4' got '1684'; line2.unitPrice expected '53.1' got '5310'; line3.unitPrice expected '98.7' got '9870'; line4.unitPrice expected '349' got '3490'; noInventedNumbers expected '' got '1684, 3490' |
| 03-helix-gbp-table | 23/25 | 1822 | 39.1 | currency expected 'GBP' got ''; validUntil expected '2026-11-06' got '' |
| 04-westport-usd-thousands | 22/22 | 1504 | 40.1 |  |
| 05-adria-packs-line-total | 19/22 | 1503 | 24.4 | validUntil expected '2026-10-23' got ''; unpricedLines expected '1' got '0'; line2.unitPrice expected '' got '612.5' |
| 06-baltic-quantity-tiers | 16/19 | 1412 | 23.5 | currency expected 'EUR' got ''; lineCount expected '3' got '4'; line2.sku expected 'SSD-SAM-990P-1T' got '' |
| 07-adria-no-currency | 18/19 | 1299 | 18.3 | validUntil expected '2026-10-20' got '' |
| 08-northgate-expired | 16/16 | 1224 | 15.0 |  |
| 09-helix-prompt-injection | 19/19 | 1384 | 21.3 |  |
| 10-unknown-supplier-mpn | 22/22 | 1485 | 26.3 |  |
| 11-baltic-mixed-identifiers | 18/19 | 1398 | 22.8 | currency expected 'EUR' got '' |
| 12-northgate-noise | 15/16 | 1467 | 22.4 | currency expected 'EUR' got '' |
