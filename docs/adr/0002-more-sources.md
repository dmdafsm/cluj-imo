# ADR 0002 — Additional sources: OSM, INS, city hall site, payments, SICAP

**Status:** accepted · 2026-10-30

## Context
Permits alone place work only at street level, miss the metropolitan area (where much of the
housing is built), show intent only from the urbanism certificate on, and say nothing about
public investment.

## Decision
| Source | What it adds | Refresh |
|---|---|---|
| OpenStreetMap (Overpass) | ~20k address points → house-level placement; neighborhood polygons; construction sites | weekly |
| INS TEMPO (LOC104B, LOC108B, LOC101B) | dwellings completed, residential permits and stock for every locality of the county | weekly |
| primariaclujnapoca.ro | PUZ/PUD intent tables (PDF, PdfPig) and Local Council decisions (HTML) | daily |
| data.gov.ro payments | city hall payments, works flagged by description | daily |
| data.gov.ro SICAP exports | procurement contracts of CUI 4305857 (xls/xlsx/csv/ods) | weekly, each file once |

Each is an `ISourceIngester`; parsing lives in pure functions with fixture tests.

## Consequences
- Overpass public instances are often busy: the client retries and falls back between
  endpoints, and treats 200-with-error-remark as a failure. A self-hosted Overpass is the fix if
  it becomes a problem.
- Scrapers depend on the city hall site markup and PDF layout; failures surface as failed runs
  on the Data page.
- SICAP files are large (up to ~300 MB); only years from `Investments:SicapFromYear` are read.
- Private persons appear in several sources; they are reduced to "Persoană fizică" at ingestion.
