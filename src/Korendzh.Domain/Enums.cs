namespace Korendzh.Domain;

/// <summary>
/// Application roles. Mirrored as ASP.NET Identity roles ("Admin", "Manager", "Worker").
/// See docs/roles-permissions.md.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Worker = "Worker";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Manager, Worker };
}

public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Restored = 4
}

public enum NotificationChannel
{
    Email = 1,
    Push = 2
}

public enum NotificationStatus
{
    Queued = 1,
    Sent = 2,
    Failed = 3
}

public enum PushPlatform
{
    iOS = 1,
    Android = 2
}
