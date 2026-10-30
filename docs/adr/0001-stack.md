# ADR 0001 — .NET + Razor Pages + htmx + Beer CSS + SQLite

**Status:** accepted · 2026-10-30

## Context
The owner prefers the .NET ecosystem and wants as little JavaScript as possible, a
Material look, and everything runnable in Docker. Data volume is small (~25k rows/year
across datasets), read-heavy, refreshed once a day.

## Decision
- ASP.NET Core Razor Pages renders HTML; htmx swaps partials for filters, search and paging.
- Beer CSS provides Material Design 3 with semantic HTML and no build step.
- Leaflet is the only JS library, for the map (no reasonable HTML-only alternative).
- SQLite via EF Core: zero-ops, a single file on a volume, fast enough for this size.

## Consequences
- No SPA, no bundler, no node in the image.
- If data grows or needs spatial queries, move to PostgreSQL/PostGIS (EF provider swap).
