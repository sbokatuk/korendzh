using System.Globalization;

namespace Korendzh.Infrastructure.Auth;

/// <summary>
/// Нормализует email к ASCII/Punycode.
/// Проблема: браузер для &lt;input type="email"&gt; конвертирует IDN-домен в Punycode перед сабмитом
/// (admin@бокатюк.бел → admin@xn--80ab1abr0a8f.xn--90ais), поэтому если в БД хранится кириллический
/// email, сравнение не пройдёт. Решение — всегда приводить к ASCII при сохранении и при поиске.
/// </summary>
public static class EmailNormalizer
{
    private static readonly IdnMapping Idn = new();

    /// <summary>
    /// Возвращает email с ASCII-доменом (Punycode) и localpart как есть.
    /// Если входное значение пустое или не содержит '@' — возвращается как есть.
    /// </summary>
    public static string ToAscii(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return email ?? string.Empty;
        var trimmed = email.Trim();
        var at = trimmed.IndexOf('@');
        if (at <= 0 || at == trimmed.Length - 1) return trimmed;

        var localPart = trimmed[..at];
        var domain = trimmed[(at + 1)..];

        try
        {
            var asciiDomain = Idn.GetAscii(domain);
            return localPart + "@" + asciiDomain;
        }
        catch (ArgumentException)
        {
            // Невалидный домен — возвращаем исходник, пусть валидация на следующем шаге его отклонит.
            return trimmed;
        }
    }
}
