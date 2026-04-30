namespace Korendzh.Web.Configuration;

/// <summary>
/// Режим работы приложения.
/// См. docs/app-mode.md.
/// </summary>
public enum AppMode
{
    /// <summary>Полный режим: публичный лендинг СТО + CMS + личный кабинет сотрудников.</summary>
    Full = 1,

    /// <summary>Только трекинг времени, статистика и управление пользователями. Без публичного сайта и CMS.</summary>
    TrackingOnly = 2
}

public class AppOptions
{
    public AppMode Mode { get; set; } = AppMode.Full;

    public bool IsTrackingOnly => Mode == AppMode.TrackingOnly;
    public bool IsFull => Mode == AppMode.Full;
}
