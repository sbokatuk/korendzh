using System.Globalization;

namespace Korendzh.Infrastructure.Plan;

/// <summary>
/// Размер бакета для агрегации «План vs Факт» на оси X.
/// </summary>
public enum PlanGranularity
{
    Day = 1,
    Month = 2,
    /// <summary>Скользящие 3 месяца от даты начала диапазона (anchor).</summary>
    ThreeMonths = 3,
    /// <summary>Календарный квартал (Q1: Янв–Мар, Q2: Апр–Июн, ...).</summary>
    Quarter = 4,
    /// <summary>Полугодие — H1: Янв–Июн, H2: Июл–Дек.</summary>
    HalfYear = 5,
    Year = 6
}

/// <summary>
/// Бакет агрегата плана/факта.
/// </summary>
public record PlanBucket(string Label, DateOnly StartDate, decimal PlannedHours, decimal ActualHours);

public static class PlanGranularityExtensions
{
    private static readonly CultureInfo Ru = new("ru-RU");

    public static string Label(this PlanGranularity g) => g switch
    {
        PlanGranularity.Day => "По дням",
        PlanGranularity.Month => "По месяцам",
        PlanGranularity.ThreeMonths => "По 3 месяца",
        PlanGranularity.Quarter => "По кварталам",
        PlanGranularity.HalfYear => "По полугодиям",
        PlanGranularity.Year => "По годам",
        _ => g.ToString()
    };

    /// <summary>
    /// Сгруппировать суточные данные в бакеты согласно гранулярности.
    /// anchor — обычно дата начала диапазона; используется для шаблона «3 месяца» (rolling от anchor).
    /// </summary>
    public static List<PlanBucket> GroupByGranularity(
        this IEnumerable<PlanVsActualDay> days,
        PlanGranularity granularity,
        DateOnly anchor)
    {
        var buckets = new SortedDictionary<DateOnly, (string Label, decimal Planned, decimal Actual)>();

        foreach (var day in days)
        {
            var (start, label) = GetBucket(day.Date, granularity, anchor);

            if (buckets.TryGetValue(start, out var existing))
            {
                buckets[start] = (existing.Label, existing.Planned + day.PlannedHours, existing.Actual + day.ActualHours);
            }
            else
            {
                buckets[start] = (label, day.PlannedHours, day.ActualHours);
            }
        }

        return buckets
            .Select(kv => new PlanBucket(kv.Value.Label, kv.Key, kv.Value.Planned, kv.Value.Actual))
            .ToList();
    }

    private static (DateOnly BucketStart, string Label) GetBucket(DateOnly date, PlanGranularity g, DateOnly anchor)
    {
        switch (g)
        {
            case PlanGranularity.Day:
                return (date, date.ToString("d MMM", Ru));

            case PlanGranularity.Month:
            {
                var start = new DateOnly(date.Year, date.Month, 1);
                return (start, Capitalize(start.ToString("LLLL yyyy", Ru)));
            }

            case PlanGranularity.ThreeMonths:
            {
                // Скользящий бакет от anchor.
                var anchorMonth = new DateOnly(anchor.Year, anchor.Month, 1);
                var monthsSinceAnchor = (date.Year - anchorMonth.Year) * 12 + (date.Month - anchorMonth.Month);
                var idx = (monthsSinceAnchor < 0 ? (monthsSinceAnchor - 2) : monthsSinceAnchor) / 3;
                var start = anchorMonth.AddMonths(idx * 3);
                var end = start.AddMonths(3).AddDays(-1);
                var label = $"{Capitalize(start.ToString("LLL yy", Ru))}–{Capitalize(end.ToString("LLL yy", Ru))}";
                return (start, label);
            }

            case PlanGranularity.Quarter:
            {
                var q = (date.Month - 1) / 3 + 1;
                var start = new DateOnly(date.Year, (q - 1) * 3 + 1, 1);
                return (start, $"Q{q} {date.Year}");
            }

            case PlanGranularity.HalfYear:
            {
                var h = date.Month <= 6 ? 1 : 2;
                var start = new DateOnly(date.Year, h == 1 ? 1 : 7, 1);
                return (start, $"H{h} {date.Year}");
            }

            case PlanGranularity.Year:
            {
                var start = new DateOnly(date.Year, 1, 1);
                return (start, date.Year.ToString());
            }

            default:
                return (date, date.ToString("yyyy-MM-dd"));
        }
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], Ru) + s[1..];
}
