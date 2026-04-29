namespace Korendzh.Infrastructure.Auth;

public interface IPasswordResetService
{
    /// <summary>
    /// Запросить сброс пароля по email. Если пользователь существует и активен —
    /// создаётся токен и ставится в очередь email. Возвращает true в любом случае,
    /// чтобы не раскрывать существование аккаунта.
    /// </summary>
    Task<bool> RequestAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Подтвердить сброс: установить новый пароль по сырому токену.
    /// </summary>
    Task<bool> ConfirmAsync(string rawToken, string newPassword, CancellationToken ct = default);
}
