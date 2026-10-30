using ClujImo.Domain.Permits;

namespace ClujImo.Domain.Tests;

public sealed class SourceDateTests
{
    [Theory]
    [InlineData("08.01.2025", 2025, 1, 8)]
    [InlineData("10/10/2022, nr. 143 din 28/11/2019", 2022, 10, 10)]
    [InlineData(" 27/05/2022 ", 2022, 5, 27)]
    public void Parses_source_formats(string raw, int year, int month, int day) =>
        Assert.Equal(new DateOnly(year, month, day), SourceDate.Parse(raw));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("-")]
    [InlineData("32.13.2025")]
    public void Invalid_dates_are_null(string? raw) => Assert.Null(SourceDate.Parse(raw));
}
