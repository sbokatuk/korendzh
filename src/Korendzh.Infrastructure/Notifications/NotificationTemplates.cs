namespace Korendzh.Infrastructure.Notifications;

/// <summary>
/// Стабильные теги шаблонов уведомлений. См. docs/notifications.md.
/// </summary>
public static class NotificationTemplates
{
    public const string InviteCreated = "invite.created";
    public const string PasswordResetRequested = "password.reset_requested";
    public const string PasswordChanged = "password.changed";
    public const string TimeEntryEditedByManager = "timeentry.edited_by_manager";
    public const string TimeEntryDeletedByManager = "timeentry.deleted_by_manager";
    public const string TimeEntryCreatedByManager = "timeentry.created_by_manager";
}
