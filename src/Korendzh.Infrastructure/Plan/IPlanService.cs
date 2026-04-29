using Korendzh.Domain.Plan;

namespace Korendzh.Infrastructure.Plan;

/// <summary>Точка одного дня — план + факт суммарно.</summary>
public record PlanVsActualDay(DateOnly Date, decimal PlannedHours, decimal ActualHours);

public interface IPlanService
{
    /// <summary>План воркера за период.</summary>
    Task<List<PlanEntry>> GetPlanAsync(Guid workerId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Создать или обновить запись плана. Если hours == 0 — запись удаляется.</summary>
    Task UpsertAsync(Guid workerId, DateOnly date, decimal hours, Guid actorId, CancellationToken ct = default);

    /// <summary>Массовая правка одного воркера: словарь дата → часы.</summary>
    Task SaveBatchAsync(Guid workerId, IReadOnlyDictionary<DateOnly, decimal> hoursByDate, Guid actorId, CancellationToken ct = default);

    /// <summary>
    /// Заполнить план группе воркеров по шаблону. Опционально удаляет существующие записи в диапазоне до применения.
    /// </summary>
    Task BulkFillAsync(
        IEnumerable<Guid> workerIds,
        DateOnly from,
        DateOnly to,
        SchedulePattern pattern,
        decimal hoursPerDay,
        bool replaceExisting,
        Guid actorId,
        CancellationToken ct = default);

    /// <summary>
    /// Агрегат «план vs факт» по дням: поядно plannedHours и actualHours за каждый день в диапазоне.
    /// Если workerId указан — только по нему. Если нет, но указан divisionId — по всем воркерам подразделения.
    /// Если ни то ни другое — глобально по всем активным пользователям.
    /// </summary>
    Task<List<PlanVsActualDay>> GetPlanVsActualAsync(
        Guid? divisionId,
        Guid? workerId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);
}
