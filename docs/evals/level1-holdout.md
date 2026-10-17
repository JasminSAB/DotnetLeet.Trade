# Offer extraction evals, 2026-10-17 21:29 UTC

Model llama3.2:latest, instructions offer-extraction-v3, 5 cases, 78/89 fields (88%), 0 cases fully correct, 6415 tokens, 101 s.

| Case | Fields | Tokens | Seconds | Failures |
|---|---|---|---|---|
| h1-northgate-decimal-comma | 17/19 | 1385 | 22.8 | currency expected 'EUR' got ''; validUntil expected '2026-11-15' got '' |
| h2-westport-informal-sentence | 13/16 | 1107 | 12.0 | currency expected 'USD' got ''; deliveryDays expected '' got '24'; line1.sku expected 'NET-UBQ-U6-PRO' got '' |
| h3-helix-mpn-line-total | 20/22 | 1445 | 28.1 | currency expected 'GBP' got ''; validUntil expected '' got '2026-10-20' |
| h4-unknown-sender-own-skus | 14/16 | 1144 | 18.8 | validUntil expected '' got '2026-10-20'; paymentTermsDays expected '' got '0' |
| h5-baltic-forwarded-thread | 14/16 | 1334 | 19.8 | validUntil expected '' got '2026-10-20'; deliveryDays expected '3' got '2' |
