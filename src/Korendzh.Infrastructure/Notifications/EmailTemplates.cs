using System.Text.Json;

namespace Korendzh.Infrastructure.Notifications;

/// <summary>
/// Простейшие inline-шаблоны для skeleton-версии. Полноценные шаблоны (HTML+text + локализация)
/// добавляются отдельной итерацией. См. docs/notifications.md.
/// </summary>
public static class EmailTemplates
{
    public static (string Subject, string HtmlBody) Render(string templateTag, string payloadJson)
    {
        var payload = string.IsNullOrEmpty(payloadJson)
            ? new Dictionary<string, JsonElement>()
            : (JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson) ?? new());

        string Get(string key) => payload.TryGetValue(key, out var v) ? v.ToString() ?? string.Empty : string.Empty;

        return templateTag switch
        {
            NotificationTemplates.InviteCreated => (
                "Приглашение в Korendzh",
                $"<p>Здравствуйте, {Get("fullName")}!</p>" +
                $"<p>Вас пригласили в систему Korendzh. Чтобы задать пароль и войти, перейдите по ссылке:</p>" +
                $"<p><a href=\"{Get("inviteUrl")}\">{Get("inviteUrl")}</a></p>" +
                "<p>Ссылка действительна 7 дней.</p>"
            ),
            NotificationTemplates.PasswordResetRequested => (
                "Восстановление пароля Korendzh",
                "<p>Вы запросили сброс пароля. Перейдите по ссылке, чтобы задать новый:</p>" +
                $"<p><a href=\"{Get("resetUrl")}\">{Get("resetUrl")}</a></p>" +
                "<p>Ссылка действует 1 час. Если вы не запрашивали сброс — проигнорируйте письмо.</p>"
            ),
            NotificationTemplates.PasswordChanged => (
                "Пароль Korendzh изменён",
                "<p>Пароль вашего аккаунта только что был изменён. Если это были не вы — немедленно сбросьте пароль.</p>"
            ),
            NotificationTemplates.TimeEntryEditedByManager => (
                "Менеджер изменил вашу запись",
                $"<p>Запись от {Get("workDate")} (часы: {Get("hours")}) была отредактирована.</p>" +
                $"<p><a href=\"{Get("entryUrl")}\">Открыть запись</a></p>"
            ),
            NotificationTemplates.TimeEntryDeletedByManager => (
                "Менеджер удалил вашу запись",
                $"<p>Запись от {Get("workDate")} (часы: {Get("hours")}) была удалена менеджером.</p>"
            ),
            NotificationTemplates.TimeEntryCreatedByManager => (
                "Менеджер добавил запись от вашего имени",
                $"<p>Создана запись от {Get("workDate")} (часы: {Get("hours")}).</p>" +
                $"<p><a href=\"{Get("entryUrl")}\">Открыть запись</a></p>"
            ),
            _ => ("Уведомление Korendzh", $"<p>Событие: {templateTag}</p>")
        };
    }
}

public static class PushTemplates
{
    public static string Render(string templateTag, string payloadJson) => templateTag switch
    {
        NotificationTemplates.TimeEntryEditedByManager => "Менеджер изменил вашу запись",
        NotificationTemplates.TimeEntryDeletedByManager => "Менеджер удалил вашу запись",
        NotificationTemplates.TimeEntryCreatedByManager => "Менеджер создал запись от вашего имени",
        _ => "Korendzh: уведомление"
    };
}
