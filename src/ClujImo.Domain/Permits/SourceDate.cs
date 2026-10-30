using System.Globalization;

namespace ClujImo.Domain.Permits;

public static class SourceDate
{
    private static readonly string[] Formats = ["dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];

    public static DateOnly? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Some fields carry trailing notes ("10/10/2022, nr. 143 din 28/11/2019"); the first date wins.
        var head = value.Trim().Split([',', ' ', ';'], 2, StringSplitOptions.RemoveEmptyEntries)[0];
        return DateOnly.TryParseExact(head, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }
}
