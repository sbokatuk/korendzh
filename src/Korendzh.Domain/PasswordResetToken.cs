namespace Korendzh.Domain;

/// <summary>
/// Токен сброса пароля. TTL 1 час. Хранится только хэш.
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);

    public DateTime? ConsumedAt { get; set; }

    public bool IsValid(DateTime nowUtc) => ConsumedAt == null && ExpiresAt > nowUtc;
}
