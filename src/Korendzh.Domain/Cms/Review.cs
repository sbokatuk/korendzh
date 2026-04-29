namespace Korendzh.Domain.Cms;

/// <summary>
/// Отзыв клиента. Добавляется админом вручную (см. cms.md).
/// </summary>
public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Текст отзыва.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Оценка 1..5. Если 0 — не показывать звёзды.</summary>
    public int Rating { get; set; } = 5;

    /// <summary>Опциональное фото автора (через MediaAsset).</summary>
    public string? AuthorPhotoUrl { get; set; }

    /// <summary>Дата отзыва (как опубликован клиентом, не дата записи в БД).</summary>
    public DateOnly ReviewDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public bool IsPublished { get; set; } = true;

    public int DisplayOrder { get; set; } = 100;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedById { get; set; }
}
