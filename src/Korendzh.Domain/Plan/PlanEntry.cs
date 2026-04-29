namespace Korendzh.Domain.Plan;

/// <summary>
/// Запись плана: сколько часов работы запланировано у пользователя в конкретный день.
/// Уникальный ключ — пара (WorkerId, WorkDate).
/// См. docs/plan.md.
/// </summary>
public class PlanEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Пользователь (воркер или менеджер), для которого запланированы часы.</summary>
    public Guid WorkerId { get; set; }

    public DateOnly WorkDate { get; set; }

    /// <summary>Запланировано часов на этот день. 0 допускается (нерабочий день).</summary>
    public decimal PlannedHours { get; set; }

    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? UpdatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
