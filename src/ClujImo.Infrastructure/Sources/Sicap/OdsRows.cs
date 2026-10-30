using System.IO.Compression;
using System.Xml;

namespace ClujImo.Infrastructure.Sources.Sicap;

/// <summary>
/// Minimal streaming reader for OpenDocument spreadsheets (first sheet only): yields each row's cell texts.
/// Some SICAP quarters are published as .ods; files are large, so content.xml is never loaded whole.
/// </summary>
public static class OdsRows
{
    private const string TableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
    private const string OfficeNs = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";

    /// <summary>Trailing empty cells are often "repeated" thousands of times; nothing useful lies past this.</summary>
    private const int MaxRepeat = 64;

    public static IEnumerable<string?[]> Read(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var entry = zip.GetEntry("content.xml") ?? throw new InvalidDataException("Not an OpenDocument spreadsheet.");
        using var content = entry.Open();
        using var xml = XmlReader.Create(content, new XmlReaderSettings { IgnoreWhitespace = true, DtdProcessing = DtdProcessing.Prohibit });

        var tables = 0;
        while (xml.Read())
        {
            if (xml.NodeType != XmlNodeType.Element || xml.NamespaceURI != TableNs)
            {
                continue;
            }

            if (xml.LocalName == "table" && ++tables > 1)
            {
                yield break;
            }

            if (xml.LocalName == "table-row" && !xml.IsEmptyElement)
            {
                var row = ReadRow(xml.ReadSubtree());
                if (row.Any(c => c is not null))
                {
                    yield return row;
                }
            }
        }
    }

    private static string?[] ReadRow(XmlReader row)
    {
        var cells = new List<string?>();
        row.Read();
        while (row.Read())
        {
            if (row.NodeType != XmlNodeType.Element || row.NamespaceURI != TableNs || row.LocalName is not ("table-cell" or "covered-table-cell"))
            {
                continue;
            }

            var repeat = int.TryParse(row.GetAttribute("number-columns-repeated", TableNs), out var r) ? Math.Min(r, MaxRepeat) : 1;
            var value = row.GetAttribute("date-value", OfficeNs) ?? row.GetAttribute("value", OfficeNs);
            var text = row.IsEmptyElement ? null : CellText(row.ReadSubtree());
            for (var i = 0; i < repeat; i++)
            {
                cells.Add(value ?? text);
            }
        }

        return [.. cells];
    }

    private static string? CellText(XmlReader cell)
    {
        var parts = new List<string>();
        while (cell.Read())
        {
            if (cell.NodeType is XmlNodeType.Text or XmlNodeType.SignificantWhitespace)
            {
                parts.Add(cell.Value);
            }
        }

        return parts.Count == 0 ? null : string.Concat(parts);
    }
}
