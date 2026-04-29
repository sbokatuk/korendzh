using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Plan;

[Authorize(Policy = Auth.AuthorizationPolicies.ManagerOrAdmin)]
public class WorkersModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;

    public WorkersModel(AppDbContext db, UserManager<AppUser> users)
    {
        _db = db;
        _users = users;
    }

    public List<AppUser> Items { get; private set; } = new();
    public Dictionary<Guid, string> DivisionNames { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return;

        var isAdmin = await _users.IsInRoleAsync(actor, Roles.Admin);

        IQueryable<AppUser> q = _db.Users.Where(u => u.IsActive);
        if (!isAdmin)
        {
            q = q.Where(u => u.DivisionId == actor.DivisionId);
        }

        Items = await q.OrderBy(u => u.FullName).ToListAsync();
        DivisionNames = await _db.Divisions.ToDictionaryAsync(d => d.Id, d => d.Name);
    }
}
