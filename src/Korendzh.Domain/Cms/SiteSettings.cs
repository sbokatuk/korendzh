namespace Korendzh.Domain.Cms;

/// <summary>
/// Настройки публичного сайта (singleton — одна запись).
/// Редактируется админом через /Admin/SiteSettings.
/// </summary>
public class SiteSettings
{
    public int Id { get; set; } = 1;

    public string SiteName { get; set; } = "Korendzh";

    /// <summary>Заголовок hero на главной.</summary>
    public string HeroTitle { get; set; } = "СТО Korendzh — ремонт без сюрпризов";

    /// <summary>Подзаголовок под hero.</summary>
    public string HeroSubtitle { get; set; } =
        "Диагностика, ремонт двигателя и подвески, замена расходников. Работаем с легковыми и коммерческими авто.";

    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string WorkingHours { get; set; } = "Пн–Пт: 9:00–19:00, Сб: 10:00–16:00, Вс: выходной";

    /// <summary>URL фото для hero (через MediaAsset). Если null — используется CSS-градиент.</summary>
    public string? HeroImageUrl { get; set; }

    public string? InstagramUrl { get; set; }
    public string? TelegramUrl { get; set; }
    public string? VkUrl { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedById { get; set; }
}
