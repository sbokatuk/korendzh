using System.Text.Encodings.Web;
using System.Text.Json;
using Korendzh.Domain;
using Korendzh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Infrastructure.Notifications;

public class NotificationDispatcher : INotificationDispatcher
{
    /// <summary>
    /// Не эскейпим не-ASCII (кириллицу и пр.) в JSON, чтобы payload в БД и логах оставался читаемым.
    /// Безопасно: JSON используется как сырой стораж + рендер через шаблоны, никогда не вставляется в HTML напрямую.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

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
            PayloadJson = JsonSerializer.Serialize(payload, JsonOpts),
            Status = NotificationStatus.Queued,
            NextAttemptAt = DateTime.UtcNow,
        };

        _db.Notifications.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
