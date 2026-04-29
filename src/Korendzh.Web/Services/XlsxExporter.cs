using ClosedXML.Excel;
using Korendzh.Infrastructure.Services;

namespace Korendzh.Web.Services;

public static class XlsxExporter
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Export(IEnumerable<StatBucket> buckets, string title, string headerLabel = "Метка")
    {
        using var wb = new XLWorkbook();
        var sheet = wb.Worksheets.Add("Статистика");

        sheet.Cell(1, 1).Value = title;
        sheet.Range(1, 1, 1, 2).Merge().Style.Font.Bold = true;

        sheet.Cell(2, 1).Value = headerLabel;
        sheet.Cell(2, 2).Value = "Часы";
        var header = sheet.Range(2, 1, 2, 2);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 3;
        decimal total = 0;
        foreach (var b in buckets)
        {
            sheet.Cell(row, 1).Value = b.Label;
            sheet.Cell(row, 2).Value = (double)b.Hours;
            sheet.Cell(row, 2).Style.NumberFormat.Format = "0.##";
            total += b.Hours;
            row++;
        }

        if (row > 3)
        {
            sheet.Cell(row, 1).Value = "Итого";
            sheet.Cell(row, 2).Value = (double)total;
            var totalRange = sheet.Range(row, 1, row, 2);
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}
