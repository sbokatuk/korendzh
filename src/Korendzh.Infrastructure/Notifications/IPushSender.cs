using Korendzh.Domain;

namespace Korendzh.Infrastructure.Notifications;

public interface IPushSender
{
    Task SendAsync(PushDevice device, string title, string body, CancellationToken ct = default);
}
