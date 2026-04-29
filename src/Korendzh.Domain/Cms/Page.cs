namespace Korendzh.Domain.Cms;

/// <summary>
/// Произвольная редактируемая текстовая страница: «О нас», «Гарантии», «Доставка».
/// Доступна по адресу /p/{Slug}.
/// </summary>
public class Page
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Слаг URL: /p/{slug}. Латиница/цифры/дефис.</summary>
    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>Содержимое страницы в HTML.</summary>
    public string ContentHtml { get; set; } = string.Empty;

    /// <summary>Если true — страница появляется в публичном меню.</summary>
    public bool ShowInMenu { get; set; }

    public int MenuOrder { get; set; } = 100;

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
}
