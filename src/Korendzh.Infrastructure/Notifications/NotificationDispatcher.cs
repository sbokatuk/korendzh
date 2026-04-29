using System.Text.Json;
using Korendzh.Domain;
using Korendzh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Infrastructure.Notifications;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly AppDbContext _db;

    public NotificationDispatcher(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnqueueAsync(
        Guid userId,
        NotificationChannel channel,
        string templateTag,
        string eventKey,
        object payload,
        CancellationToken ct = default)
    {
        var existing = await _db.Notifications
            .AsNoTracking()
            .AnyAsync(x => x.EventKey == eventKey, ct);

        if (existing) return;

        var entry = new NotificationLogEntry
        {
            UserId = userId,
            Channel = channel,
            TemplateTag = templateTag,
            EventKey = eventKey,
            PayloadJson = JsonSerializer.Serialize(payload),
            Status = NotificationStatus.Queued,
            NextAttemptAt = DateTime.UtcNow,
        };

        _db.Notifications.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
