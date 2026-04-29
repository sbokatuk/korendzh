namespace Korendzh.Web.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "korendzh";
    public string Audience { get; set; } = "korendzh-clients";
    /// <summary>
    /// Секретный ключ. В проде задаётся через переменную окружения Jwt:Key (Plesk Application Settings).
    /// Минимум 32 символа для HS256.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 60 * 24; // 24 часа.
}
