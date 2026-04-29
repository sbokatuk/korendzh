using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Korendzh.Infrastructure.Notifications;

/// <summary>
/// Фоновый воркер: периодически забирает Queued-уведомления из БД, отправляет (email через SMTP, push заглушкой),
/// прописывает Sent/Failed и planирует ретраи. Идемпотентность поддерживается на уровне постановки в очередь.
/// </summary>
public class NotificationSenderService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(12)
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationSenderService> _log;

    public NotificationSenderService(IServiceScopeFactory scopeFactory, ILogger<NotificationSenderService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatch(stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Notification sender batch failed");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task ProcessBatch(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var pushSender = scope.ServiceProvider.GetRequiredService<IPushSender>();

        var nowUtc = DateTime.UtcNow;

        var batch = await db.Notifications
            .Where(n => n.Status == NotificationStatus.Queued
                        && (n.NextAttemptAt == null || n.NextAttemptAt <= nowUtc))
            .OrderBy(n => n.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        foreach (var n in batch)
        {
            try
            {
                if (n.Channel == NotificationChannel.Email)
                {
                    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == n.UserId, ct);
                    if (user is null || string.IsNullOrEmpty(user.Email))
                    {
                        n.Status = NotificationStatus.Failed;
                        n.FailureReason = "User has no email";
                    }
                    else if (!user.EmailNotificationsEnabled && !IsTransactional(n.TemplateTag))
                    {
                        n.Status = NotificationStatus.Sent;
                        n.SentAt = DateTime.UtcNow;
                        n.FailureReason = "Skipped: user disabled email notifications";
                    }
                    else
                    {
                        var (subject, body) = EmailTemplates.Render(n.TemplateTag, n.PayloadJson);
                        await emailSender.SendAsync(user.Email, subject, body, ct);
                        n.Status = NotificationStatus.Sent;
                        n.SentAt = DateTime.UtcNow;
                    }
                }
                else if (n.Channel == NotificationChannel.Push)
                {
                    var devices = await db.PushDevices
                        .Where(d => d.UserId == n.UserId && d.IsActive)
                        .ToListAsync(ct);

                    foreach (var d in devices)
                    {
                        await pushSender.SendAsync(d, "Korendzh", PushTemplates.Render(n.TemplateTag, n.PayloadJson), ct);
                    }
                    n.Status = NotificationStatus.Sent;
                    n.SentAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                n.AttemptCount += 1;
                n.FailureReason = ex.Message;

                if (n.AttemptCount >= RetryDelays.Length)
                {
                    n.Status = NotificationStatus.Failed;
                    _log.LogError(ex, "Notification {Id} permanently failed", n.Id);
                }
                else
                {
                    n.NextAttemptAt = DateTime.UtcNow + RetryDelays[n.AttemptCount];
                    _log.LogWarning(ex, "Notification {Id} will retry at {At}", n.Id, n.NextAttemptAt);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static bool IsTransactional(string tag) =>
        tag is NotificationTemplates.InviteCreated
            or NotificationTemplates.PasswordResetRequested
            or NotificationTemplates.PasswordChanged;
}
