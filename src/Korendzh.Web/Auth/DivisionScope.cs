using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Auth;

/// <summary>
/// Хелпер: проверить, что текущий пользователь имеет право работать с указанным воркером
/// (для менеджера — только в своём подразделении, для админа — всегда true).
/// </summary>
public class DivisionScope
{
    private readonly UserManager<AppUser> _users;
    private readonly AppDbContext _db;

    public DivisionScope(UserManager<AppUser> users, AppDbContext db)
    {
        _users = users;
        _db = db;
    }

    public async Task<bool> CanAccessWorkerAsync(AppUser actor, Guid workerId)
    {
        if (await _users.IsInRoleAsync(actor, Roles.Admin)) return true;

        if (await _users.IsInRoleAsync(actor, Roles.Manager))
        {
            var worker = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == workerId);
            return worker?.DivisionId == actor.DivisionId && actor.DivisionId.HasValue;
        }

        // Worker сам = себе.
        return actor.Id == workerId;
    }

    public async Task<bool> CanAccessTimeEntryAsync(AppUser actor, Guid workerId)
        => await CanAccessWorkerAsync(actor, workerId);
}
