using Korendzh.Domain;
using Korendzh.Infrastructure.Notifications;
using Korendzh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Infrastructure.Services;

public class TimeEntryService : ITimeEntryService
{
    private readonly AppDbContext _db;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ICarService _cars;

    public TimeEntryService(AppDbContext db, INotificationDispatcher dispatcher, ICarService cars)
    {
        _db = db;
        _dispatcher = dispatcher;
        _cars = cars;
    }

    public async Task<TimeEntry> CreateAsync(TimeEntry entry, Guid actorId, CancellationToken ct = default)
    {
        // CreatedBy/CreatedAt проставит AuditingInterceptor, но если actorId известен — фиксируем сразу.
        if (entry.CreatedById == Guid.Empty) entry.CreatedById = actorId;
        if (entry.CreatedAt == default) entry.CreatedAt = DateTime.UtcNow;

        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync(ct);

        // Если запись создана не самим воркером — уведомляем воркера.
        if (entry.WorkerId != actorId)
        {
            await NotifyWorker(entry, NotificationTemplates.TimeEntryCreatedByManager, ct);
        }

        return entry;
    }

    public async Task<TimeEntry?> UpdateAsync(TimeEntry update, Guid actorId, CancellationToken ct = default)
    {
        var existing = await _db.TimeEntries.FirstOrDefaultAsync(x => x.Id == update.Id, ct);
        if (existing is null) return null;

        existing.WorkDate = update.WorkDate;
        existing.Hours = update.Hours;
        existing.TaskName = update.TaskName;
        existing.CarId = update.CarId;
        existing.LicensePlate = update.LicensePlate;
        existing.Description = update.Description;

        existing.UpdatedById = actorId;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        if (existing.WorkerId != actorId)
        {
            await NotifyWorker(existing, NotificationTemplates.TimeEntryEditedByManager, ct);
        }

        return existing;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var existing = await _db.TimeEntries.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null) return false;

        existing.IsDeleted = true;
        existing.DeletedById = actorId;
        existing.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        if (existing.WorkerId != actorId)
        {
            await NotifyWorker(existing, NotificationTemplates.TimeEntryDeletedByManager, ct);
        }
        return true;
    }

    private async Task NotifyWorker(TimeEntry entry, string templateTag, CancellationToken ct)
    {
        var payload = new
        {
            workDate = entry.WorkDate.ToString("yyyy-MM-dd"),
            hours = entry.Hours.ToString("0.##"),
            entryUrl = $"/TimeEntries/Details/{entry.Id}",
        };

        // Email + push идут двумя записями с разными ключами.
        var key = $"{templateTag}:{entry.Id}:{(entry.UpdatedAt ?? entry.CreatedAt).Ticks}";
        await _dispatcher.EnqueueAsync(entry.WorkerId, NotificationChannel.Email, templateTag, $"{key}:email", payload, ct);
        await _dispatcher.EnqueueAsync(entry.WorkerId, NotificationChannel.Push, templateTag, $"{key}:push", payload, ct);
    }
}
