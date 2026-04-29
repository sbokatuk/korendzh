namespace Korendzh.Domain.Cms;

/// <summary>
/// Услуга СТО (карточка на лендинге).
/// </summary>
public class Service
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Слаг для URL: /services/{slug}. Латиница, цифры, дефисы.</summary>
    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>Краткое описание (1–2 строки) для карточки в списке.</summary>
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>Полное описание (HTML) для детальной страницы.</summary>
    public string DescriptionHtml { get; set; } = string.Empty;

    /// <summary>Цена: «от 50 руб.», «по запросу» — свободный текст.</summary>
    public string PriceLabel { get; set; } = string.Empty;

    /// <summary>URL картинки (через MediaAsset), null — заглушка.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Порядок отображения: меньше — выше. Дефолт 100.</summary>
    public int DisplayOrder { get; set; } = 100;

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
}
