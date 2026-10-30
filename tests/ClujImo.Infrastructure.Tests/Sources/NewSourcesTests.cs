using System.Text.Json;
using ClujImo.Domain.Planning;
using ClujImo.Infrastructure.Ingestion.Sources;
using ClujImo.Infrastructure.Sources;
using ClujImo.Infrastructure.Sources.CityHall;
using ClujImo.Infrastructure.Sources.Ins;
using ClujImo.Infrastructure.Sources.Osm;
using ClujImo.Infrastructure.Sources.Sicap;

namespace ClujImo.Infrastructure.Tests.Sources;

public sealed class NewSourcesTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    [Fact]
    public void Overpass_addresses_split_multi_numbers_and_skip_untagged()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Fixture("overpass-sample.json")));

        var addresses = OsmParser.Addresses(json);

        Assert.Equal(["16", "20", "22"], addresses.Select(a => a.HouseNumber).Order());
        Assert.All(addresses, a => Assert.Equal("ALBERT EINSTEIN", a.StreetKey));
    }

    [Fact]
    public void Overpass_neighborhood_polygon_wins_over_point_with_same_name()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Fixture("overpass-sample.json")));

        var areas = OsmParser.Neighborhoods(json);

        Assert.True(areas.Single(a => a.Name == "Gheorgheni").HasPolygon);
        Assert.False(areas.Single(a => a.Name == "Micro 1").HasPolygon);
        Assert.Single(OsmParser.ConstructionSites(json), s => s.Building == "apartments");
    }

    [Fact]
    public void Tempo_table_reads_years_units_and_siruta()
    {
        string[][] rows =
        [
            ["Surse de finantare", "Judete", "Localitati ", "Ani", "Ani"],
            ["Surse de finantare", "Judete", "Localitati ", "Anul 2024", "Anul 2025"],
            ["Surse de finantare", "Judete", "Localitati ", "UM: Numar", "UM: Numar"],
            ["Surse de finantare", "Judete", "Localitati ", "Numar", "Numar"],
            ["Total", "Cluj", "54975 MUNICIPIUL CLUJ-NAPOCA", "1640", "957"],
            ["Total", "Cluj", "57706 FLORESTI", "1488", ":"],
        ];

        var values = TempoTable.Read(rows, localityColumn: 2);

        Assert.Equal(3, values.Count);
        Assert.Contains(new TempoValue("54975", "Cluj-Napoca", 2025, "Numar", 957), values);
        Assert.Contains(values, v => v.Locality == "Floresti" && v.Year == 2024);
    }

    [Fact]
    public void City_hall_decision_list_is_parsed()
    {
        var items = CityHallPages.Decisions(File.ReadAllText(Fixture("hcl-list.html")));

        Assert.Equal(6, items.Count);
        Assert.All(items, i => Assert.Equal(2026, i.Year));
        Assert.Contains(items, i => i.Title.StartsWith("Aprobarea P.U.Z.", StringComparison.Ordinal) && i.Date is not null);
        Assert.Single(CityHallPages.IntentTables(File.ReadAllText(Fixture("hcl-list.html"))));
    }

    [Fact]
    public void Sicap_row_maps_and_works_are_recognized()
    {
        var row = new Dictionary<string, string>
        {
            ["AUTORITATE CONTRACTANTA"] = "Municipiul Cluj-Napoca",
            ["TIP CONTRACT"] = "Lucrari",
            ["TIP PROCEDURA"] = "Procedura simplificata",
            ["COD CPV"] = "45310000-3",
            ["DENUMIRE CPV"] = "Lucrari de instalatii electrice (Rev.2)",
            ["NUMAR LOT"] = "1",
            ["NUMAR ANUNT ATRIBUIRE"] = "SCNA1126352",
            ["DATA CONTRACT"] = "06/10/2025 00:00:00",
            ["NUMAR CONTRACT"] = "784145",
            ["VALOARE CONTRACT (RON)"] = "3482314",
            ["OFERTANT CASTIGATOR"] = "EXEMPLU INSTAL",
            ["CUI OFERTANT CASTIGATOR"] = "RO123",
        };

        var contract = SicapReader.Map(name => row.GetValueOrDefault(name), "4305857")!;

        Assert.Equal("45310000", contract.CpvCode);
        Assert.Equal(new DateOnly(2025, 10, 6), contract.ContractDate);
        Assert.Equal(3482314m, contract.ValueRon);
        Assert.True(contract.IsWorks);
    }

    [Fact]
    public void Sicap_prefers_excel_over_csv_of_the_same_report_and_skips_unreadable_and_subsequent_contracts()
    {
        static CkanResource R(string name, string file) => new(name, new Uri($"https://data.gov.ro/x/{file}"), 2024);

        var files = SicapIngester.ContractFiles([
            R("Contracte publicate T I 2024", "t1.csv"),
            R("Contracte publicate T I 2024", "t1.xls"),
            R("Raport Contracte publicate T II 2024", "t2.csv"),
            R("Contracte subsecvente 2024 - T1", "s1.xlsx"),
            R("Achizitii directe T1", "d1.xlsx"),
            R("Contracte T III", "t3.ods"),
            R("Contracte T IV", "t4.zip"),
        ]).Select(r => r.Url.Segments[^1]).ToList();

        Assert.Equal(["t1.xls", "t2.csv", "t3.ods"], files);
    }

    [Fact]
    public void Payments_get_stable_distinct_keys_and_hide_private_beneficiaries()
    {
        static IReadOnlyDictionary<string, string?> Row(string who, string what) => new Dictionary<string, string?>
        {
            ["DATA_EMITERII"] = "2025-01-13", ["BENEFICIAR"] = who, ["SUMA"] = "165.00", ["REPREZENTAND"] = what,
        };

        var payments = PaymentsIngester.Map([
            Row("EXEMPLU CONSTRUCT SRL", "lucrari modernizare str. Exemplu"),
            Row("EXEMPLU CONSTRUCT SRL", "lucrari modernizare str. Exemplu"),
            Row("POPESCU ION", "despagubire"),
        ]);

        Assert.Equal(3, payments.Select(p => p.RowKey).Distinct().Count());
        Assert.True(payments[0].IsWorks);
        Assert.Equal("Persoană fizică", payments[2].Beneficiary);
        Assert.False(payments[2].IsWorks);
    }

    [Fact]
    public void Sicap_reads_csv_with_2024_column_names()
    {
        const string csv = """
            Autoritate contractanta,CUI,Tip contract,Cod CPV,Denumire CPV,Numar lot,Numar anunt atribuire,Data contract,Numar contract,Valoare contract (RON),Ofertant,CUI ofertant
            Judetul Brasov,4384150,Furnizare,31625200-5,Sisteme de alarma,1,CAN1,11/12/2023 00:00:00,117,540253,EXEMPLU SRL,1
            Municipiul Cluj-Napoca,4305857,Lucrari,45233140-2,Lucrari de drumuri,1,CAN2,06/02/2024 00:00:00,A-1,1000000,EXEMPLU DRUMURI SRL,RO2
            """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        var contracts = SicapReader.Read(stream, ".csv", new HashSet<string> { "4305857" }).ToList();

        var contract = Assert.Single(contracts);
        Assert.Equal("EXEMPLU DRUMURI SRL", contract.Winner);
        Assert.Equal(new DateOnly(2024, 2, 6), contract.ContractDate);
    }

    [Fact]
    public void Plan_kind_is_read_from_decision_title() =>
        Assert.Equal(UrbanPlanKind.Pud, DecisionClassifier.PlanKind("Aprobarea P.U.D. – construire hotel"));
}
