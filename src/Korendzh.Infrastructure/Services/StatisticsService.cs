using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Infrastructure.Services;

public class StatisticsService : IStatisticsService
{
    private readonly AppDbContext _db;

    public StatisticsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<StatBucket>> HoursByWorkerAsync(Guid? divisionId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var entries = _db.TimeEntries.Where(e => e.WorkDate >= from && e.WorkDate <= to);
        var users = _db.Users.AsQueryable();
        if (divisionId.HasValue) users = users.Where(u => u.DivisionId == divisionId);

        var query = from e in entries
                    join u in users on e.WorkerId equals u.Id
                    group e by new { u.Id, u.FullName } into g
                    select new StatBucket
                    {
                        Key = g.Key.Id.ToString(),
                        Label = g.Key.FullName,
                        Hours = g.Sum(x => x.Hours)
                    };

        return await query.OrderByDescending(b => b.Hours).ToListAsync(ct);
    }

    public async Task<List<StatBucket>> HoursByTaskAsync(Guid? divisionId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var entries = _db.TimeEntries.Where(e => e.WorkDate >= from && e.WorkDate <= to);
        if (divisionId.HasValue)
        {
            entries = from e in entries
                      join u in _db.Users on e.WorkerId equals u.Id
                      where u.DivisionId == divisionId
                      select e;
        }

        var query = entries
            .GroupBy(e => e.TaskName)
            .Select(g => new StatBucket
            {
                Key = g.Key,
                Label = g.Key,
                Hours = g.Sum(x => x.Hours)
            });

        return await query.OrderByDescending(b => b.Hours).ToListAsync(ct);
    }

    public async Task<List<StatBucket>> HoursByCarAsync(Guid? divisionId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var entries = _db.TimeEntries.Where(e => e.WorkDate >= from && e.WorkDate <= to);
        if (divisionId.HasValue)
        {
            entries = from e in entries
                      join u in _db.Users on e.WorkerId equals u.Id
                      where u.DivisionId == divisionId
                      select e;
        }

        var query = from e in entries
                    join c in _db.Cars on e.CarId equals c.Id into gj
                    from c in gj.DefaultIfEmpty()
                    let label = c != null ? c.Name : (e.LicensePlate ?? "(без авто)")
                    group e by label into g
                    select new StatBucket
                    {
                        Key = g.Key,
                        Label = g.Key,
                        Hours = g.Sum(x => x.Hours)
                    };

        return await query.OrderByDescending(b => b.Hours).ToListAsync(ct);
    }
}
