namespace Korendzh.Domain;

/// <summary>
/// Справочник автомобилей. Может пополняться воркерами через автокомплит.
/// См. docs/data-model.md и docs/validation.md.
/// </summary>
public class Car
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? LicensePlate { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid CreatedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
