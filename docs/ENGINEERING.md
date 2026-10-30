# Engineering guidelines — cluj-imo

These rules are the contract for every change in this repository. When code and this
document disagree, fix one of them in the same change.

## 1. What we are building

A self-hosted app that ingests the open construction datasets published by Primăria
Cluj-Napoca on data.gov.ro (building permits / AC, urbanism certificates / CU, completion
certificates / atestare, street nomenclator), classifies and geolocates them, and shows
**what is being built, where, and how it trends**.

## 2. Stack (decided — see `docs/adr/`)

| Concern | Choice |
|---|---|
| Runtime | .NET 10, C# latest, ASP.NET Core |
| UI | Razor Pages, server-rendered HTML, **htmx** for interactivity |
| Design system | **Material Design 3 via Beer CSS** (no custom component library) |
| JavaScript | Only where HTML cannot do it (the Leaflet map). No bundler, no npm at build time |
| Storage | SQLite through EF Core, file on a Docker volume |
| Parsing | PdfPig (PDF tables), ExcelDataReader (SICAP xls/xlsx/csv), own ODS reader |
| Tests | xUnit v3 |
| Delivery | Single Docker image, `docker compose up` |

Front-end libraries are **vendored** under `src/ClujImo.Web/wwwroot/lib/<name>-<version>/`
with exact versions. The app must run with no outbound CDN access.

## 3. Solution layout and dependency rule

```
src/
  ClujImo.Domain/          pure model + parsing/classification logic. No I/O, no packages.
  ClujImo.Infrastructure/  EF Core, HTTP clients (CKAN, Nominatim), ingestion, queries.
  ClujImo.Web/             Razor Pages, htmx partials, hosted services, composition root.
tests/
  ClujImo.Domain.Tests/
  ClujImo.Infrastructure.Tests/
```

Dependencies point inward only: `Web → Infrastructure → Domain`. Domain never references
EF, ASP.NET or `HttpClient`. If a rule about the data (how an address is parsed, how a
permit is classified) lives anywhere but Domain, it is in the wrong place.

## 4. C# conventions

- `Nullable` enabled, `TreatWarningsAsErrors` on, analyzers at `latest-recommended`.
  Do not suppress a warning without a comment saying why.
- File-scoped namespaces, one public type per file, `sealed` by default.
- Prefer `record`/`record struct` for values, immutable where possible.
- Async all the way; every async I/O method takes a `CancellationToken` and passes it on.
- No static mutable state. Time comes from `TimeProvider`, never `DateTime.Now`.
- Dates from the source are `DateOnly`. Parse with `CultureInfo.InvariantCulture` and
  explicit formats; never rely on the server culture.
- Options via `IOptions<T>` bound from configuration sections, validated on start
  (`ValidateDataAnnotations().ValidateOnStart()`).
- Logging: structured (`logger.LogInformation("Ingested {Count} rows from {Dataset}", ...)`),
  no string interpolation in log templates. Use `LoggerMessage` source generators for
  hot paths.
- Names in code are English. User-facing text is Romanian.

## 5. Data rules

- **Raw is sacred.** Every row keeps its original fields (`RawAddress`, `Description`, ...).
  Derived columns (street, category, levels, units, coordinates) are recomputed from raw
  and can be rebuilt at any time without re-downloading.
- Ingestion is **idempotent**: natural key `(Kind, Year, Number)`; re-running upserts.
- The source is messy (raw control characters in JSON, BOMs, mixed diacritics, `nr. FN`,
  "zona str. X"). Tolerate it in parsing, never "fix" it by hand in the database.
- Classification is rule-based and deterministic, in `ClujImo.Domain.Classification`.
  Every new rule ships with a test that uses a **real description copied from the data**.
- External services are polite: Nominatim max 1 request/second, identifying
  `User-Agent`, results cached forever in the `Streets` table. Geocoding can be turned
  off with `Geocoding:Enabled=false`.
- **Every source is an `ISourceIngester`** with its own name, refresh interval and on/off
  switch. The orchestrator records one `IngestionRun` per source; one failing source never
  blocks the others. Enrichment (classification, placement, neighborhoods) runs once after
  all due sources, over all tables.
- Snapshots that replace data (OSM) check that the new snapshot is sane before deleting the
  old one. Large immutable files (SICAP quarters, monthly PDFs) are recorded in
  `ProcessedResources` and never downloaded twice.
- Scraped sources (city hall site, PDFs) are parsed by pure functions over HTML or positioned
  words, so tests run on saved fixtures. A format change must fail loudly (exception, failed
  run), not silently import nothing.
- **Privacy.** The app is about what gets built, not about individuals. Names of private
  persons (applicants, plan initiators, payment beneficiaries) are never stored: run them
  through `PartyName.Display`, which keeps organizations and returns "Persoană fizică"
  otherwise. Fixtures committed to the repo contain no real personal names.
- Schema changes go through EF Core migrations (`dotnet ef migrations add <Name>`
  from the repo root, see README). Migrations apply on startup.

## 6. UI rules (htmx + Material)

- A page works as a plain full-page GET first. htmx then enhances it: filters use
  `hx-get` + `hx-target` + `hx-push-url="true"`, so every view has a shareable URL.
- Partials live next to their page as `_Name.cshtml`. A handler returns the partial when
  the request has the `HX-Request` header, the full page otherwise.
- Use Beer CSS elements and helpers (`article`, `nav`, `field`, `chip`, `table`, ...) before
  writing CSS. Custom CSS goes in `wwwroot/css/site.css`, uses Material tokens
  (`var(--primary)`, `var(--surface-container)`, ...) and stays small.
- Light and dark mode both work; never hard-code colors.
- Charts are server-rendered (SVG partials or plain HTML/CSS bars). JS charting libraries need an ADR.
- Beer CSS owns many short class names (`fill`, `stripes`, `small`, ...). Prefix custom classes (`bar-fill`) to avoid collisions.
- Mobile first: every page usable at 360px wide.
- Accessibility: every control has a label, icons that carry meaning have `aria-label`,
  colors are never the only signal.

## 7. Testing

- Domain logic: unit tests, table-driven (`[Theory]` + `[InlineData]`) with real samples.
- Infrastructure: parsing tests against small fixture files in `tests/**/Fixtures/`
  (trimmed copies of real source files, including the broken ones).
- No test hits the network.
- `dotnet test` must pass before every commit; CI runs the same inside Docker.

## 8. Docker

- Multi-stage `Dockerfile`: SDK image builds and runs tests, `aspnet` image runs.
- Runs as the non-root `app` user, listens on `8080`, exposes `/health`.
- State only in the `/data` volume. Everything else in the container is disposable.
- Config only via environment variables (`ConnectionStrings__Default`, `Geocoding__Enabled`, ...).

## 9. Change workflow

- Small, focused changes; one concern per commit, imperative commit subjects.
- A behaviour change updates tests; an architecture change adds an ADR in `docs/adr/`.
- Keep `README.md` runnable: the commands in it must work on a clean clone.
