using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Notifications;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Korendzh.Infrastructure.Auth;

public class InviteService : IInviteService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly INotificationDispatcher _dispatcher;
    private readonly EmailOptions _emailOpt;

    public InviteService(
        AppDbContext db,
        UserManager<AppUser> users,
        INotificationDispatcher dispatcher,
        IOptions<EmailOptions> emailOpt)
    {
        _db = db;
        _users = users;
        _dispatcher = dispatcher;
        _emailOpt = emailOpt.Value;
    }

    public async Task<AppUser> CreateInviteAsync(
        string email,
        string fullName,
        string role,
        Guid? divisionId,
        Guid invitedById,
        CancellationToken ct = default)
    {
        // Всегда нормализуем email к ASCII (Punycode-домен), чтобы хранение совпадало с тем,
        // что присылает браузер на формах входа.
        email = EmailNormalizer.ToAscii(email);

        var existing = await _users.FindByEmailAsync(email);
        if (existing != null)
        {
            throw new InvalidOperationException($"Пользователь с email {email} уже существует.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName,
            DivisionId = divisionId,
            EmailConfirmed = false,
            IsActive = true,
        };

        // Создаём пользователя без пароля; он задаст его при принятии инвайта.
        var createResult = await _users.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        var roleResult = await _users.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        var (raw, hash) = TokenHasher.NewToken();
        var invite = new InvitationToken
        {
            UserId = user.Id,
            TokenHash = hash,
            CreatedById = invitedById,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        _db.InvitationTokens.Add(invite);
        await _db.SaveChangesAsync(ct);

        var inviteUrl = $"{_emailOpt.AppBaseUrl.TrimEnd('/')}/Account/AcceptInvite?token={Uri.EscapeDataString(raw)}";

        await _dispatcher.EnqueueAsync(
            user.Id,
            NotificationChannel.Email,
            NotificationTemplates.InviteCreated,
            $"invite:{invite.Id}",
            new { fullName = user.FullName, inviteUrl },
            ct);

        return user;
    }

    public async Task<AppUser?> AcceptInviteAsync(string rawToken, string newPassword, CancellationToken ct = default)
    {
        var hash = TokenHasher.Hash(rawToken);
        var token = await _db.InvitationTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null || !token.IsValid(DateTime.UtcNow)) return null;

        var user = await _users.FindByIdAsync(token.UserId.ToString());
        if (user is null) return null;

        // Если пароль уже стоит — снимаем; затем выставляем новый.
        if (await _users.HasPasswordAsync(user))
        {
            var removed = await _users.RemovePasswordAsync(user);
            if (!removed.Succeeded)
                throw new InvalidOperationException(string.Join("; ", removed.Errors.Select(e => e.Description)));
        }

        var addResult = await _users.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
            throw new InvalidOperationException(string.Join("; ", addResult.Errors.Select(e => e.Description)));

        user.EmailConfirmed = true;
        await _users.UpdateAsync(user);

        token.ConsumedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return user;
    }
}
