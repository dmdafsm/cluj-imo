# Cluj Imo

Ce se construiește în Cluj-Napoca, unde și cum evoluează. Datele vin din seturile deschise ale Primăriei
pe [data.gov.ro](https://data.gov.ro/organization/primaria-municipiului-cluj-napoca):

- autorizații de construire (AC), certificate de urbanism (CU), certificate de atestare a edificării, 2022 → prezent
- nomenclatorul stradal (nume oficiale, zonă fiscală)
- coordonate și cartier per stradă din OpenStreetMap / Nominatim

Aplicația descarcă totul, clasifică fiecare lucrare după descriere (bloc, casă, semicolectiv, birouri, hală,
renovare, extindere, demolare, rețele...), extrage regimul de înălțime și numărul de unități, și arată:

- **Sumar**: indicatori pe an, comparații cu aceeași perioadă a anului trecut, ritm lunar pe ani,
  tipuri de lucrări, cartiere și străzi fierbinți, „ce urmează” (CU → AC → atestare)
- **Hartă**: străzi cu lucrări, filtrabile
- **Explorează**: căutare și filtrare în toate documentele, cu detalii
- **Date**: starea actualizării și a geolocalizării

## Rulare

```bash
docker compose up -d --build
```

Deschide http://localhost:8080. La prima pornire baza e goală și se descarcă automat (~30 s). Geolocalizarea
străzilor rulează în fundal la ~1 cerere/secundă (~20 de minute prima dată), cartierele și harta se completează
pe parcurs. Datele stau în volumul `data`.

Alt port: `CLUJ_IMO_PORT=8088 docker compose up -d`.

Dacă rulezi o instanță publică, pune un contact în `Geocoding__UserAgent` (cerut de
[politica Nominatim](https://operations.osmfoundation.org/policies/nominatim/)) sau dezactivează geocodarea
cu `Geocoding__Enabled=false`.

## Dezvoltare

Stack: .NET 10, Razor Pages + htmx, Material Design 3 (Beer CSS), Leaflet, SQLite/EF Core.
Citește [docs/ENGINEERING.md](docs/ENGINEERING.md) înainte de modificări.

```bash
dotnet test
dotnet run --project src/ClujImo.Web
```

Migrare nouă după schimbări de model:

```bash
dotnet tool restore
dotnet ef migrations add <Nume> --project src/ClujImo.Infrastructure --startup-project src/ClujImo.Web --output-dir Persistence/Migrations
```

Configurare (variabile de mediu): `ConnectionStrings__Default`, `OpenData__RefreshInterval`,
`OpenData__RefreshOnStartup`, `Geocoding__Enabled`, `Geocoding__UserAgent`.

## Structură

```
src/ClujImo.Domain          parsare adrese, clasificare lucrări (fără I/O)
src/ClujImo.Infrastructure  EF Core, client CKAN, ingestie, geocodare, interogări
src/ClujImo.Web             pagini Razor, partiale htmx, workeri de fundal
tests/                      xUnit, cu exemple reale din date
```

## Licențe

Cod: [MIT](LICENSE). Datele: Primăria Municipiului Cluj-Napoca, [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/), via data.gov.ro.
Hărți și geocodare: © OpenStreetMap contributors (ODbL). Biblioteci terțe: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
