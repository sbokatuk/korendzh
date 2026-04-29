namespace Korendzh.Domain;

/// <summary>
/// Запись audit log. Пишется EF Core SaveChangesInterceptor для значимых сущностей.
/// См. docs/data-model.md.
/// </summary>
public class AuditLogEntry
{
    public long Id { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public AuditAction Action { get; set; }

    public Guid? ActorId { get; set; }

    public DateTime At { get; set; } = DateTime.UtcNow;

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }
}
