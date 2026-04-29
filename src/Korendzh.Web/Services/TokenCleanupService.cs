using Korendzh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Services;

/// <summary>
/// Регулярно удаляет просроченные/использованные InvitationToken и PasswordResetToken.
/// Альтернатива внешнему Plesk Scheduled Task — фоновый сервис в самом приложении.
/// </summary>
public class TokenCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenCleanupService> _log;

    public TokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<TokenCleanupService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Один раз сразу после старта, далее по интервалу.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Token cleanup batch failed");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task CleanAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var nowUtc = DateTime.UtcNow;

        // Удаляем токены с истёкшим ExpiresAt либо использованные более 7 дней назад.
        var threshold = nowUtc.AddDays(-7);

        var invitesRemoved = await db.InvitationTokens
            .Where(t => t.ExpiresAt < nowUtc || (t.ConsumedAt != null && t.ConsumedAt < threshold))
            .ExecuteDeleteAsync(ct);

        var resetsRemoved = await db.PasswordResetTokens
            .Where(t => t.ExpiresAt < nowUtc || (t.ConsumedAt != null && t.ConsumedAt < threshold))
            .ExecuteDeleteAsync(ct);

        if (invitesRemoved + resetsRemoved > 0)
        {
            _log.LogInformation("Cleaned tokens: invites={Inv}, resets={Res}", invitesRemoved, resetsRemoved);
        }
    }
}
