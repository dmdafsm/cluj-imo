using ClujImo.Infrastructure.Sources;

namespace ClujImo.Infrastructure.Tests.Sources;

public sealed class LenientJsonTests
{
    private static byte[] Fixture(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void Reads_file_with_raw_control_characters_inside_strings()
    {
        var rows = LenientJson.ReadRows(Fixture("atestare_2022_broken.json"));

        Assert.Equal(2, rows.Count);
        Assert.Equal("514", rows[1]["NrCertificat"]);
        Assert.DoesNotContain('\n', rows[1]["ExistaConstructie"]!);
    }

    [Fact]
    public void Reads_file_with_byte_order_mark() =>
        Assert.Equal(2, LenientJson.ReadRows(Fixture("cu_2025.json")).Count);

    [Fact]
    public void Rejects_non_array_documents() =>
        Assert.Throws<FormatException>(() => LenientJson.ReadRows("{\"a\":1}"u8));
}
