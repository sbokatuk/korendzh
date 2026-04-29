using Korendzh.Domain;

namespace Korendzh.Infrastructure.Notifications;

/// <summary>
/// Постановка уведомлений в очередь. Реальная отправка — фоновым воркером (см. NotificationSender).
/// Идемпотентность — через EventKey: повторная постановка с тем же ключом игнорируется.
/// </summary>
public interface INotificationDispatcher
{
    Task EnqueueAsync(
        Guid userId,
        NotificationChannel channel,
        string templateTag,
        string eventKey,
        object payload,
        CancellationToken ct = default);
}
