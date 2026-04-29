namespace Korendzh.Infrastructure.Auditing;

/// <summary>
/// Текущий аутентифицированный пользователь, доступный из инфраструктурного слоя
/// (например, из EF SaveChangesInterceptor). Реализация — в Web-проекте поверх HttpContextAccessor.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
