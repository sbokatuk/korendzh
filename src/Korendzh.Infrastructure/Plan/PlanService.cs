using Korendzh.Domain.Plan;
using Korendzh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Infrastructure.Plan;

public class PlanService : IPlanService
{
    private readonly AppDbContext _db;

    public PlanService(AppDbContext db) { _db = db; }

    public async Task<List<PlanEntry>> GetPlanAsync(Guid workerId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        return await _db.PlanEntries
            .AsNoTracking()
            .Where(p => p.WorkerId == workerId && p.WorkDate >= from && p.WorkDate <= to)
            .OrderBy(p => p.WorkDate)
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(Guid workerId, DateOnly date, decimal hours, Guid actorId, CancellationToken ct = default)
    {
        var existing = await _db.PlanEntries
            .FirstOrDefaultAsync(p => p.WorkerId == workerId && p.WorkDate == date, ct);

        if (hours <= 0m)
        {
            if (existing is not null)
            {
                _db.PlanEntries.Remove(existing);
                await _db.SaveChangesAsync(ct);
            }
            return;
        }

        if (existing is null)
        {
            _db.PlanEntries.Add(new PlanEntry
            {
                WorkerId = workerId,
                WorkDate = date,
                PlannedHours = hours,
                CreatedById = actorId,
            });
        }
        else
        {
            existing.PlannedHours = hours;
            existing.UpdatedById = actorId;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveBatchAsync(Guid workerId, IReadOnlyDictionary<DateOnly, decimal> hoursByDate, Guid actorId, CancellationToken ct = default)
    {
        if (hoursByDate.Count == 0) return;

        var dates = hoursByDate.Keys.ToList();
        var existing = await _db.PlanEntries
            .Where(p => p.WorkerId == workerId && dates.Contains(p.WorkDate))
            .ToListAsync(ct);
        var existingByDate = existing.ToDictionary(p => p.WorkDate);

        foreach (var (date, hours) in hoursByDate)
        {
            existingByDate.TryGetValue(date, out var entry);

            if (hours <= 0m)
            {
                if (entry is not null) _db.PlanEntries.Remove(entry);
                continue;
            }

            if (entry is null)
            {
                _db.PlanEntries.Add(new PlanEntry
                {
                    WorkerId = workerId,
                    WorkDate = date,
                    PlannedHours = hours,
                    CreatedById = actorId,
                });
            }
            else
            {
                entry.PlannedHours = hours;
                entry.UpdatedById = actorId;
                entry.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task BulkFillAsync(
        IEnumerable<Guid> workerIds,
        DateOnly from,
        DateOnly to,
        SchedulePattern pattern,
        decimal hoursPerDay,
        bool replaceExisting,
        Guid actorId,
        CancellationToken ct = default)
    {
        if (to < from) throw new ArgumentException("Дата 'до' должна быть не раньше 'от'.");
        if (hoursPerDay <= 0 || hoursPerDay > 24) throw new ArgumentException("Часов в день должно быть от 0.25 до 24.");

        var ids = workerIds.Distinct().ToList();
        if (ids.Count == 0) return;

        // 1. Удаляем существующие записи в диапазоне (если просили).
        if (replaceExisting)
        {
            await _db.PlanEntries
                .Where(p => ids.Contains(p.WorkerId) && p.WorkDate >= from && p.WorkDate <= to)
                .ExecuteDeleteAsync(ct);
        }

        // 2. Генерируем рабочие дни по шаблону.
        var workDays = pattern.EnumerateWorkDays(from, to).ToList();
        if (workDays.Count == 0)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        // 3. Если не replaceExisting — нужно знать, что уже есть, чтобы не дублировать.
        Dictionary<(Guid, DateOnly), PlanEntry> existing = new();
        if (!replaceExisting)
        {
            var rows = await _db.PlanEntries
                .Where(p => ids.Contains(p.WorkerId) && p.WorkDate >= from && p.WorkDate <= to)
                .ToListAsync(ct);
            existing = rows.ToDictionary(p => (p.WorkerId, p.WorkDate));
        }

        // 4. Создаём/обновляем.
        foreach (var workerId in ids)
        {
            foreach (var date in workDays)
            {
                if (existing.TryGetValue((workerId, date), out var entry))
                {
                    entry.PlannedHours = hoursPerDay;
                    entry.UpdatedById = actorId;
                    entry.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _db.PlanEntries.Add(new PlanEntry
                    {
                        WorkerId = workerId,
                        WorkDate = date,
                        PlannedHours = hoursPerDay,
                        CreatedById = actorId,
                    });
                }
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<PlanVsActualDay>> GetPlanVsActualAsync(
        Guid? divisionId,
        Guid? workerId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        // Список воркеров для агрегата.
        IQueryable<Guid> workerIdsQ;
        if (workerId.HasValue)
        {
            workerIdsQ = new[] { workerId.Value }.AsQueryable();
        }
        else if (divisionId.HasValue)
        {
            workerIdsQ = _db.Users.Where(u => u.DivisionId == divisionId).Select(u => u.Id);
        }
        else
        {
            workerIdsQ = _db.Users.Select(u => u.Id);
        }

        var workerIds = await workerIdsQ.ToListAsync(ct);

        var planByDate = await _db.PlanEntries
            .Where(p => workerIds.Contains(p.WorkerId) && p.WorkDate >= from && p.WorkDate <= to)
            .GroupBy(p => p.WorkDate)
            .Select(g => new { Date = g.Key, Hours = g.Sum(x => x.PlannedHours) })
            .ToDictionaryAsync(g => g.Date, g => g.Hours, ct);

        var actualByDate = await _db.TimeEntries
            .Where(e => workerIds.Contains(e.WorkerId) && e.WorkDate >= from && e.WorkDate <= to)
            .GroupBy(e => e.WorkDate)
            .Select(g => new { Date = g.Key, Hours = g.Sum(x => x.Hours) })
            .ToDictionaryAsync(g => g.Date, g => g.Hours, ct);

        var result = new List<PlanVsActualDay>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            planByDate.TryGetValue(d, out var planned);
            actualByDate.TryGetValue(d, out var actual);
            result.Add(new PlanVsActualDay(d, planned, actual));
        }
        return result;
    }
}
