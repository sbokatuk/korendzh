using System.Globalization;
using System.Text;

namespace Korendzh.Infrastructure.Services;

/// <summary>
/// Простейший CSV-экспортер для статистики. Excel/xlsx — следующей итерацией (через ClosedXML / EPPlus).
/// </summary>
public static class CsvExporter
{
    public static byte[] Export(IEnumerable<StatBucket> buckets, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\"{title}\"");
        sb.AppendLine("Метка;Часы");
        foreach (var b in buckets)
        {
            sb.Append('"').Append(Escape(b.Label)).Append('"').Append(';');
            sb.AppendLine(b.Hours.ToString("0.##", CultureInfo.InvariantCulture));
        }
        // BOM для корректного открытия в Excel.
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var combined = new byte[bom.Length + content.Length];
        Buffer.BlockCopy(bom, 0, combined, 0, bom.Length);
        Buffer.BlockCopy(content, 0, combined, bom.Length, content.Length);
        return combined;
    }

    private static string Escape(string s) => s.Replace("\"", "\"\"");
}
