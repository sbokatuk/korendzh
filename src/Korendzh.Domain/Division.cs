namespace Korendzh.Domain;

/// <summary>
/// Подразделение. Один менеджер = одно подразделение. См. docs/data-model.md.
/// </summary>
public class Division
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Менеджер, ответственный за подразделение. Может быть null временно — например, после деактивации
    /// прежнего менеджера, до назначения нового. На активное подразделение всегда должен быть назначен менеджер.
    /// </summary>
    public Guid? ManagerId { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
