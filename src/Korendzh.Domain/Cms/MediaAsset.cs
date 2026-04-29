namespace Korendzh.Domain.Cms;

/// <summary>
/// Реестр загруженных файлов. Физически файлы лежат в wwwroot/uploads/yyyy/MM/.
/// Используется для управления медиа из админки и для подсчёта осиротевших файлов.
/// </summary>
public class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Относительный URL для отображения, например /uploads/2026/04/abc123.jpg.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Имя файла, как загрузил пользователь.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>MIME-тип, валидируется при загрузке.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Размер в байтах.</summary>
    public long SizeBytes { get; set; }

    /// <summary>SHA-256 содержимого. Используется как имя файла на диске для идемпотентности.</summary>
    public string ContentHash { get; set; } = string.Empty;

    public Guid UploadedById { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
