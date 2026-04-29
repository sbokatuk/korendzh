namespace Korendzh.Domain;

/// <summary>
/// Токен приглашения нового пользователя. TTL по умолчанию 7 дней. Хранится только хэш токена.
/// См. docs/validation.md.
/// </summary>
public class InvitationToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public Guid CreatedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    public DateTime? ConsumedAt { get; set; }

    public bool IsValid(DateTime nowUtc) => ConsumedAt == null && ExpiresAt > nowUtc;
}
