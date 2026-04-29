using Microsoft.AspNetCore.Identity;

namespace Korendzh.Infrastructure.Identity;

/// <summary>
/// Кастомный Identity-пользователь. Расширяет IdentityUser&lt;Guid&gt; полями из docs/data-model.md.
/// Роль определяется через ASP.NET Identity Roles (Admin/Manager/Worker).
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public string? GoogleSubject { get; set; }

    /// <summary>
    /// Подразделение. Для воркера — обязательно. Для менеджера — собственное (совпадает с Division.ManagerId).
    /// Для админа — null.
    /// </summary>
    public Guid? DivisionId { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// IANA-зона пользователя (например, "Europe/Minsk"). Если null — берётся системный дефолт.
    /// </summary>
    public string? TimeZone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
