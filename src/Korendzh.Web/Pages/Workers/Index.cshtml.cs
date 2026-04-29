using Korendzh.Domain;
using Korendzh.Infrastructure.Auth;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Workers;

[Authorize(Policy = Auth.AuthorizationPolicies.ManagerOrAdmin)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IPasswordResetService _resets;

    public IndexModel(AppDbContext db, UserManager<AppUser> users, IPasswordResetService resets)
    {
        _db = db;
        _users = users;
        _resets = resets;
    }

    public List<AppUser> Items { get; private set; } = new();
    public Dictionary<Guid, string> DivisionNames { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        var workerIds = await _users.GetUsersInRoleAsync(Roles.Worker);
        var ids = workerIds.Select(u => u.Id).ToHashSet();

        var q = _db.Users.Where(u => ids.Contains(u.Id));

        if (await _users.IsInRoleAsync(actor, Roles.Manager))
        {
            q = q.Where(u => u.DivisionId == actor.DivisionId);
        }

        Items = await q.OrderBy(u => u.FullName).ToListAsync();
        DivisionNames = await _db.Divisions.ToDictionaryAsync(d => d.Id, d => d.Name);
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id)
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();
        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (target is null) return RedirectToPage();
        if (await _users.IsInRoleAsync(actor, Roles.Manager) && target.DivisionId != actor.DivisionId) return Forbid();

        target.IsActive = !target.IsActive;
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = target.IsActive ? "Активирован." : "Деактивирован.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(Guid id)
    {
        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (target?.Email is null) return RedirectToPage();
        await _resets.RequestAsync(target.Email);
        TempData["StatusMessage"] = "Письмо для сброса пароля отправлено.";
        return RedirectToPage();
    }
}
