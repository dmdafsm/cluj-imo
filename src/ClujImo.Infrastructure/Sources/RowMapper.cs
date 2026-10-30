using System.Globalization;
using ClujImo.Domain.Permits;
using ClujImo.Domain.Text;

namespace ClujImo.Infrastructure.Sources;

/// <summary>Maps the raw rows of each dataset to <see cref="Permit"/>. Only raw fields are set here.</summary>
public static class RowMapper
{
    public static Permit? Map(PermitKind kind, IReadOnlyDictionary<string, string?> row, int fallbackYear) => kind switch
    {
        PermitKind.BuildingPermit => MapCommon(kind, row, fallbackYear, "nraut", "dataaut", "lucrari", "valabilitate"),
        PermitKind.UrbanismCertificate => MapCommon(kind, row, fallbackYear, "nrcert", "datacert", "scop", "valabil"),
        PermitKind.CompletionCertificate => MapCompletion(row, fallbackYear),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static Permit? MapCommon(
        PermitKind kind,
        IReadOnlyDictionary<string, string?> row,
        int fallbackYear,
        string numberField,
        string dateField,
        string descriptionField,
        string validityField)
    {
        var number = Get(row, numberField);
        if (number is null)
        {
            return null;
        }

        var validity = ParseInt(Get(row, validityField));
        var validityUnit = Get(row, "luni_zile_valab");
        if (validity is not null && validityUnit is not null && validityUnit.StartsWith("zile", StringComparison.OrdinalIgnoreCase))
        {
            validity = (int)Math.Ceiling(validity.Value / 30.0);
        }

        return new Permit
        {
            Kind = kind,
            Year = ParseInt(Get(row, "an")) ?? fallbackYear,
            Number = number,
            IssuedOn = SourceDate.Parse(Get(row, dateField)),
            RequestedOn = SourceDate.Parse(Get(row, "datacerere")),
            RequestNumber = Get(row, "nrcerere"),
            RawAddress = Get(row, "adresa_lucrare") ?? string.Empty,
            Description = TextNormalizer.Clean(Get(row, descriptionField)),
            ValidityMonths = validity,
        };
    }

    private static Permit? MapCompletion(IReadOnlyDictionary<string, string?> row, int fallbackYear)
    {
        var number = Get(row, "NrCertificat");
        if (number is null)
        {
            return null;
        }

        return new Permit
        {
            Kind = PermitKind.CompletionCertificate,
            Year = ParseInt(Get(row, "an")) ?? fallbackYear,
            Number = number,
            // The completion dataset has no address: "Adresa" holds the neighborhood.
            RawAddress = Get(row, "Adresa") ?? string.Empty,
            Description = TextNormalizer.Clean(Get(row, "ExistaConstructie")),
            ReferencedPermitNumber = Get(row, "NrAutorizatieConstructie"),
            ReferencedPermitDate = SourceDate.Parse(Get(row, "DataAutorizatieConstructie")),
        };
    }

    private static string? Get(IReadOnlyDictionary<string, string?> row, string field) =>
        row.TryGetValue(field, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    private static int? ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;
}
