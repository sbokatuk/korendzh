using Korendzh.Infrastructure.Identity;

namespace Korendzh.Infrastructure.Auth;

public interface IInviteService
{
    /// <summary>
    /// Создать пользователя в указанной роли и подразделении и поставить email-инвайт в очередь.
    /// Возвращает созданного пользователя.
    /// </summary>
    Task<AppUser> CreateInviteAsync(
        string email,
        string fullName,
        string role,
        Guid? divisionId,
        Guid invitedById,
        CancellationToken ct = default);

    /// <summary>
    /// Принять инвайт по сырому токену: проставить пароль, инвалидировать токен.
    /// </summary>
    Task<AppUser?> AcceptInviteAsync(string rawToken, string newPassword, CancellationToken ct = default);
}
