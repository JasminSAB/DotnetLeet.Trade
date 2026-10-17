# Offer extraction evals, 2026-10-08 11:09 UTC, model llama3.2:latest

| Case | Fields | Tokens | Seconds | Failures |
|---|---|---|---|---|
| 04-westport-usd-thousands | 20/22 | 1287 | 27,6 | validUntil expected '2026-10-30' got ''; deliveryDays expected '0' got '24' |
| 05-adria-packs-line-total | 19/22 | 1347 | 27,7 | validUntil expected '2026-10-23' got ''; unpricedLines expected '1' got '0'; line2.unitPrice expected '' got '612.5' |
| 03-helix-gbp-table | 24/25 | 1599 | 36,8 | validUntil expected '2026-11-06' got '' |
| 09-helix-prompt-injection | 19/19 | 1171 | 19,3 |  |
| 01-northgate-plain-list | 22/22 | 1296 | 23,3 |  |
| 08-northgate-expired | 16/16 | 1019 | 13,4 |  |
| 06-baltic-quantity-tiers | 9/19 | 1238 | 22,6 | lineCount expected '3' got '5'; unpricedLines expected '0' got '2'; line1.quantity expected '10' got ''; line1.unitPrice expected '95' got ''; line2.sku expected 'SSD-SAM-990P-1T' got ''; line2.quantity expected '50' got '10'; line2.unitPrice expected '89' got '95'; line3.sku expected 'SSD-CRU-P3P-1T' got ''; line3.quantity expected '40' got '50'; line3.unitPrice expected '53.5' got '89' |
| 12-northgate-noise | 15/16 | 1201 | 16,6 | currency expected 'EUR' got '' |
| 10-unknown-supplier-mpn | 22/22 | 1284 | 23,2 |  |
| 11-baltic-mixed-identifiers | 18/19 | 1240 | 22,0 | currency expected 'EUR' got '' |
| 02-baltic-mpn-only | 17/22 | 1288 | 23,0 | currency expected 'EUR' got ''; line1.unitPrice expected '168.4' got '1684'; line2.unitPrice expected '53.1' got '531'; line3.unitPrice expected '98.7' got '987'; noInventedNumbers expected '' got '1684, 531, 987' |
| 07-adria-no-currency | 18/19 | 1182 | 19,4 | validUntil expected '2026-10-20' got '' |
