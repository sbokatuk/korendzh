using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Notifications;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Korendzh.Infrastructure.Auth;

public class PasswordResetService : IPasswordResetService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly INotificationDispatcher _dispatcher;
    private readonly EmailOptions _emailOpt;

    public PasswordResetService(
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

    public async Task<bool> RequestAsync(string email, CancellationToken ct = default)
    {
        var user = await _users.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            // Сознательно возвращаем true — не разглашаем существование аккаунта.
            return true;
        }

        var (raw, hash) = TokenHasher.NewToken();
        var token = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        };
        _db.PasswordResetTokens.Add(token);
        await _db.SaveChangesAsync(ct);

        var resetUrl = $"{_emailOpt.AppBaseUrl.TrimEnd('/')}/Account/ResetPassword?token={Uri.EscapeDataString(raw)}";

        await _dispatcher.EnqueueAsync(
            user.Id,
            NotificationChannel.Email,
            NotificationTemplates.PasswordResetRequested,
            $"pwdreset:{token.Id}",
            new { resetUrl },
            ct);

        return true;
    }

    public async Task<bool> ConfirmAsync(string rawToken, string newPassword, CancellationToken ct = default)
    {
        var hash = TokenHasher.Hash(rawToken);
        var token = await _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null || !token.IsValid(DateTime.UtcNow)) return false;

        var user = await _users.FindByIdAsync(token.UserId.ToString());
        if (user is null) return false;

        if (await _users.HasPasswordAsync(user))
        {
            var removed = await _users.RemovePasswordAsync(user);
            if (!removed.Succeeded) return false;
        }

        var addResult = await _users.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded) return false;

        token.ConsumedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _dispatcher.EnqueueAsync(
            user.Id,
            NotificationChannel.Email,
            NotificationTemplates.PasswordChanged,
            $"pwdchanged:{Guid.NewGuid()}",
            new { },
            ct);

        return true;
    }
}
