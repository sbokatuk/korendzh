namespace Korendzh.Mobile.Services;

/// <summary>
/// Хранит JWT-токен и данные текущего пользователя в памяти процесса.
/// Сохранение между запусками — через SecureStorage (загрузка/сохранение делается явно).
/// </summary>
public class AuthState
{
    public string? Token { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public string? FullName { get; private set; }
    public string[] Roles { get; private set; } = Array.Empty<string>();

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token) && (ExpiresAtUtc is null || ExpiresAtUtc > DateTime.UtcNow);

    public void Set(string token, DateTime expiresAtUtc, string fullName, string[] roles)
    {
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        FullName = fullName;
        Roles = roles;
    }

    public void Clear()
    {
        Token = null;
        ExpiresAtUtc = null;
        FullName = null;
        Roles = Array.Empty<string>();
    }

    public async Task SaveAsync()
    {
        if (Token is null) return;
        await SecureStorage.Default.SetAsync("korendzh.token", Token);
        if (ExpiresAtUtc.HasValue)
            await SecureStorage.Default.SetAsync("korendzh.expires", ExpiresAtUtc.Value.ToString("O"));
        await SecureStorage.Default.SetAsync("korendzh.fullName", FullName ?? string.Empty);
        await SecureStorage.Default.SetAsync("korendzh.roles", string.Join(',', Roles));
    }

    public async Task LoadAsync()
    {
        var token = await SecureStorage.Default.GetAsync("korendzh.token");
        if (string.IsNullOrEmpty(token)) return;
        var expiresStr = await SecureStorage.Default.GetAsync("korendzh.expires");
        DateTime? expires = DateTime.TryParse(expiresStr, out var d) ? d : null;
        var fullName = await SecureStorage.Default.GetAsync("korendzh.fullName");
        var rolesStr = await SecureStorage.Default.GetAsync("korendzh.roles");
        var roles = string.IsNullOrEmpty(rolesStr) ? Array.Empty<string>() : rolesStr.Split(',', StringSplitOptions.RemoveEmptyEntries);

        Token = token;
        ExpiresAtUtc = expires;
        FullName = fullName;
        Roles = roles;
    }
}
