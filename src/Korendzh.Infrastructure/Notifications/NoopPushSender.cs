using Korendzh.Domain;
using Microsoft.Extensions.Logging;

namespace Korendzh.Infrastructure.Notifications;

/// <summary>
/// Заглушка push-отправителя для скелета. Реальная реализация (APNS/FCM)
/// добавляется отдельной итерацией. Пока — только пишем в лог, чтобы код пути работал.
/// </summary>
public class NoopPushSender : IPushSender
{
    private readonly ILogger<NoopPushSender> _log;

    public NoopPushSender(ILogger<NoopPushSender> log)
    {
        _log = log;
    }

    public Task SendAsync(PushDevice device, string title, string body, CancellationToken ct = default)
    {
        _log.LogInformation(
            "[PUSH STUB] platform={Platform} token=…{Tok} title={Title} body={Body}",
            device.Platform,
            device.PushToken.Length > 6 ? device.PushToken[^6..] : device.PushToken,
            title, body);
        return Task.CompletedTask;
    }
}
