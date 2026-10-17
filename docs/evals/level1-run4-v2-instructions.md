# Offer extraction evals, 2026-10-08 11:23 UTC

Model llama3.2:latest, instructions offer-extraction-v2, 12 cases, 224/243 fields (92%), 5 cases fully correct, 17408 tokens, 301 s.

| Case | Fields | Tokens | Seconds | Failures |
|---|---|---|---|---|
| 01-northgate-plain-list | 22/22 | 1474 | 24.7 |  |
| 02-baltic-mpn-only | 15/22 | 1579 | 28.6 | currency expected 'EUR' got ''; validUntil expected '' got '2026-10-20'; line1.unitPrice expected '168.4' got '1684'; line2.unitPrice expected '53.1' got '5310'; line3.unitPrice expected '98.7' got '9870'; line4.unitPrice expected '349' got '3490'; noInventedNumbers expected '' got '1684, 3490' |
| 03-helix-gbp-table | 23/25 | 1803 | 41.2 | currency expected 'GBP' got ''; validUntil expected '2026-11-06' got '' |
| 04-westport-usd-thousands | 22/22 | 1450 | 34.7 |  |
| 05-adria-packs-line-total | 18/22 | 1552 | 28.4 | currency expected 'EUR' got ''; validUntil expected '2026-10-23' got ''; unpricedLines expected '1' got '0'; line2.unitPrice expected '' got '612.5' |
| 06-baltic-quantity-tiers | 16/19 | 1388 | 23.0 | currency expected 'EUR' got ''; lineCount expected '3' got '4'; line2.sku expected 'SSD-SAM-990P-1T' got '' |
| 07-adria-no-currency | 18/19 | 1365 | 20.2 | validUntil expected '2026-10-20' got '' |
| 08-northgate-expired | 16/16 | 1197 | 13.7 |  |
| 09-helix-prompt-injection | 19/19 | 1349 | 21.0 |  |
| 10-unknown-supplier-mpn | 22/22 | 1453 | 24.3 |  |
| 11-baltic-mixed-identifiers | 18/19 | 1391 | 21.4 | currency expected 'EUR' got '' |
| 12-northgate-noise | 15/16 | 1407 | 19.2 | currency expected 'EUR' got '' |
