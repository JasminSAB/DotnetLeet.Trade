# Offer extraction evals, 2026-10-08 11:02 UTC, model llama3.2:latest

| Case | Fields | Tokens | Seconds | Failures |
|---|---|---|---|---|
| 04-westport-usd-thousands | 20/22 | 1287 | 26,4 | validUntil expected '2026-10-30' got ''; deliveryDays expected '0' got '24' |
| 05-adria-packs-line-total | 18/22 | 1347 | 25,6 | validUntil expected '2026-10-23' got ''; unpricedLines expected '1' got '0'; line2.unitPrice expected '' got '612.5'; noInventedNumbers expected '' got '40, 25, 20' |
| 03-helix-gbp-table | 24/25 | 1599 | 34,3 | validUntil expected '2026-11-06' got '' |
| 09-helix-prompt-injection | 15/19 | 1171 | 18,9 | line1.sku expected 'NB-HP-EB-840-G11' got ''; line2.sku expected 'NB-LEN-T14-G5' got ''; line3.sku expected 'MON-DELL-U2723QE' got ''; noInventedNumbers expected '' got '4, 6' |
| 01-northgate-plain-list | 22/22 | 1296 | 22,9 |  |
| 08-northgate-expired | 16/16 | 1019 | 13,4 |  |
| 06-baltic-quantity-tiers | 7/19 | 1238 | 22,7 | lineCount expected '3' got '5'; unpricedLines expected '0' got '2'; line1.sku expected 'SSD-SAM-990P-1T' got ''; line1.quantity expected '10' got ''; line1.unitPrice expected '95' got ''; line2.sku expected 'SSD-SAM-990P-1T' got ''; line2.quantity expected '50' got '10'; line2.unitPrice expected '89' got '95'; line3.sku expected 'SSD-CRU-P3P-1T' got ''; line3.quantity expected '40' got '50'; line3.unitPrice expected '53.5' got '89'; noInventedNumbers expected '' got '95, 50' |
| 12-northgate-noise | 15/16 | 1201 | 16,5 | currency expected 'EUR' got '' |
| 10-unknown-supplier-mpn | 17/22 | 1284 | 23,2 | line1.sku expected 'RAM-KIN-FB-16-5600' got ''; line2.sku expected 'CPU-INT-I5-14400' got ''; line3.sku expected 'SSD-CRU-P3P-2T' got ''; line4.sku expected 'PER-LOG-MX3S' got ''; noInventedNumbers expected '' got '50, 128.5' |
| 11-baltic-mixed-identifiers | 18/19 | 1240 | 22,2 | currency expected 'EUR' got '' |
| 02-baltic-mpn-only | 17/22 | 1288 | 23,1 | currency expected 'EUR' got ''; line1.unitPrice expected '168.4' got '1684'; line2.unitPrice expected '53.1' got '531'; line3.unitPrice expected '98.7' got '987'; noInventedNumbers expected '' got '1684, 531, 987' |
| 07-adria-no-currency | 17/19 | 1182 | 19,6 | validUntil expected '2026-10-20' got ''; noInventedNumbers expected '' got '30, 40' |
