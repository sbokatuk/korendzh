namespace Korendzh.Domain;

public class PushDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public PushPlatform Platform { get; set; }

    public string PushToken { get; set; } = string.Empty;

    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
