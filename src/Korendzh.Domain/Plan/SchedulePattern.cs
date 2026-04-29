namespace Korendzh.Domain.Plan;

/// <summary>
/// Шаблоны графика работы для автозаполнения плана.
/// </summary>
public enum SchedulePattern
{
    /// <summary>5/2: Пн–Пт работа, Сб/Вс выходные.</summary>
    StandardWeek = 1,

    /// <summary>6/1: Пн–Сб работа, Вс выходной.</summary>
    SixDayWeek = 2,

    /// <summary>2/2: 2 дня работы, 2 выходных, скользящий цикл от даты старта.</summary>
    TwoTwoShift = 3,

    /// <summary>Каждый день — рабочий.</summary>
    Daily = 4
}

/// <summary>
/// Хелперы по шаблонам — дают набор рабочих дней в диапазоне.
/// </summary>
public static class SchedulePatternExtensions
{
    /// <summary>
    /// Возвращает все рабочие даты в диапазоне [from, to] согласно шаблону.
    /// </summary>
    public static IEnumerable<DateOnly> EnumerateWorkDays(this SchedulePattern pattern, DateOnly from, DateOnly to)
    {
        if (to < from) yield break;

        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (IsWorkDay(pattern, d, cycleStart: from))
                yield return d;
        }
    }

    public static bool IsWorkDay(SchedulePattern pattern, DateOnly date, DateOnly cycleStart) => pattern switch
    {
        SchedulePattern.StandardWeek =>
            date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
        SchedulePattern.SixDayWeek =>
            date.DayOfWeek != DayOfWeek.Sunday,
        SchedulePattern.TwoTwoShift =>
            ((date.DayNumber - cycleStart.DayNumber) % 4 + 4) % 4 < 2, // первые 2 дня цикла — рабочие
        SchedulePattern.Daily => true,
        _ => false
    };

    public static string Label(this SchedulePattern p) => p switch
    {
        SchedulePattern.StandardWeek => "5/2 (Пн–Пт)",
        SchedulePattern.SixDayWeek => "6/1 (Пн–Сб)",
        SchedulePattern.TwoTwoShift => "2/2 сменный",
        SchedulePattern.Daily => "Каждый день",
        _ => p.ToString()
    };
}
