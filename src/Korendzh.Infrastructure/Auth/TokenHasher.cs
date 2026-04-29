using System.Security.Cryptography;
using System.Text;

namespace Korendzh.Infrastructure.Auth;

/// <summary>
/// Хэширование токенов (invite, password reset). В БД лежит только хэш — оригинал виден один раз в email.
/// </summary>
public static class TokenHasher
{
    /// <summary>
    /// Создать новый случайный токен (URL-safe). Возвращает (raw, hash).
    /// </summary>
    public static (string Raw, string Hash) NewToken(int byteLength = 32)
    {
        Span<byte> bytes = stackalloc byte[byteLength];
        RandomNumberGenerator.Fill(bytes);
        var raw = Base64UrlEncode(bytes);
        var hash = Hash(raw);
        return (raw, hash);
    }

    public static string Hash(string raw)
    {
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
