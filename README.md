# Cluj Imo

Ce se construiește în Cluj-Napoca, unde și cum evoluează. Datele vin din seturile deschise ale Primăriei
pe [data.gov.ro](https://data.gov.ro/organization/primaria-municipiului-cluj-napoca):

- autorizații de construire (AC), certificate de urbanism (CU), certificate de atestare a edificării, 2022 → prezent
- nomenclatorul stradal (nume oficiale, zonă fiscală)
- OpenStreetMap: adrese (poziție la nivel de imobil), contururile cartierelor, șantiere; Nominatim pentru restul străzilor
- anunțurile de intenție PUZ/PUD și hotărârile Consiliului Local (site-ul primăriei)
- INS TEMPO: locuințe terminate, autorizații și fond locativ pe toate localitățile județului
- plățile primăriei și contractele de achiziții publice (SICAP) ale Municipiului Cluj-Napoca

Aplicația descarcă totul, clasifică fiecare lucrare după descriere (bloc, casă, semicolectiv, birouri, hală,
renovare, extindere, demolare, rețele...), extrage regimul de înălțime și numărul de unități, și arată:

- **Sumar**: indicatori pe an, comparații cu aceeași perioadă a anului trecut, ritm lunar pe ani,
  tipuri de lucrări, cartiere și străzi fierbinți, „ce urmează” (CU → AC → atestare)
- **Hartă**: documente la adresă, planuri urbanistice și șantiere, filtrabile
- **Planificare**: PUZ/PUD depuse și aprobate, pe luni și cartiere
- **Investiții**: contracte de lucrări, constructori, plăți, obiective aprobate
- **Metropolă**: Cluj-Napoca față de Florești, Baciu, Apahida etc.
- **Explorează**: căutare și filtrare în toate documentele, cu detalii
- **Date**: starea actualizării și a geolocalizării

## Rulare

```bash
docker compose up -d --build
```

Deschide http://localhost:8080. La prima pornire baza e goală și se umple automat: autorizațiile în ~30 s, restul
surselor în fundal (OpenStreetMap și site-ul primăriei pot dura 10–30 de minute prima dată, SICAP descarcă fișiere
mari). Starea fiecărei surse e pe pagina **Date**. Datele stau în volumul `data`.

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
`OpenData__RefreshOnStartup`, `Geocoding__Enabled`, `Geocoding__UserAgent`, `Osm__Enabled`,
`Osm__OverpassUrls__0`, `Ins__Enabled`, `Ins__FromYear`, `CityHall__Enabled`, `CityHall__FromYear`,
`Investments__Enabled`, `Investments__SicapFromYear`, `Investments__AuthorityCuis__0`.

## Structură

```
src/ClujImo.Domain          parsare adrese, clasificare lucrări (fără I/O)
src/ClujImo.Infrastructure  EF Core, client CKAN, ingestie, geocodare, interogări
src/ClujImo.Web             pagini Razor, partiale htmx, workeri de fundal
tests/                      xUnit, cu exemple reale din date
```

## Licențe

Cod: [MIT](LICENSE). Datele: Primăria Municipiului Cluj-Napoca și Agenția pentru Agenda Digitală (SICAP), [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/),
via data.gov.ro; Institutul Național de Statistică (TEMPO); site-ul primăriei.
Hărți și geocodare: © OpenStreetMap contributors (ODbL). Biblioteci terțe: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
