using System.ComponentModel.DataAnnotations;

namespace Korendzh.Domain;

/// <summary>
/// Запись о рабочих часах. Основная транзакционная сущность системы.
/// См. docs/data-model.md, docs/validation.md.
/// </summary>
public class TimeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Воркер, к которому относится запись.
    /// </summary>
    public Guid WorkerId { get; set; }

    /// <summary>
    /// Дата выполнения работы (без времени, без зоны).
    /// </summary>
    public DateOnly WorkDate { get; set; }

    [Range(0.01, 24.0)]
    public decimal Hours { get; set; }

    [MaxLength(200)]
    public string TaskName { get; set; } = string.Empty;

    public Guid? CarId { get; set; }

    [MaxLength(20)]
    public string? LicensePlate { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public Guid CreatedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? UpdatedById { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Optimistic concurrency token. Map to SQL Server rowversion.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    public bool IsDeleted { get; set; }

    public Guid? DeletedById { get; set; }

    public DateTime? DeletedAt { get; set; }
}
