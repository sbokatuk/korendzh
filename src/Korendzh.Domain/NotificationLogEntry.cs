namespace Korendzh.Domain;

/// <summary>
/// Запись в очереди уведомлений + лог отправки.
/// См. docs/notifications.md.
/// </summary>
public class NotificationLogEntry
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public NotificationChannel Channel { get; set; }

    public string TemplateTag { get; set; } = string.Empty;

    /// <summary>
    /// Уникальный ключ события для идемпотентности.
    /// </summary>
    public string EventKey { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";

    public NotificationStatus Status { get; set; } = NotificationStatus.Queued;

    public int AttemptCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    public DateTime? NextAttemptAt { get; set; } = DateTime.UtcNow;

    public string? FailureReason { get; set; }
}
