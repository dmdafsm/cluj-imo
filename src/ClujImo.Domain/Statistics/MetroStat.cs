namespace ClujImo.Domain.Statistics;

/// <summary>One yearly value for one locality from INS TEMPO.</summary>
public sealed class MetroStat
{
    public int Id { get; set; }

    public MetroIndicator Indicator { get; set; }

    /// <summary>SIRUTA code of the locality ("54975" for Cluj-Napoca).</summary>
    public string Siruta { get; set; } = string.Empty;

    public string Locality { get; set; } = string.Empty;

    public int Year { get; set; }

    public decimal Value { get; set; }
}

public enum MetroIndicator
{
    /// <summary>LOC104B: dwellings completed during the year.</summary>
    DwellingsCompleted = 1,

    /// <summary>LOC108B: building permits for residential buildings.</summary>
    ResidentialPermits = 2,

    /// <summary>LOC108B: useful floor area authorized for residential buildings (m²).</summary>
    ResidentialPermitArea = 3,

    /// <summary>LOC101B: dwelling stock at year end.</summary>
    DwellingStock = 4,
}
