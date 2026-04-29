using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Admin.Managers;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;

    public IndexModel(AppDbContext db, UserManager<AppUser> users)
    {
        _db = db;
        _users = users;
    }

    public List<AppUser> Items { get; private set; } = new();
    public Dictionary<Guid, string> DivisionNames { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var managers = await _users.GetUsersInRoleAsync(Roles.Manager);
        Items = managers.OrderBy(u => u.FullName).ToList();
        DivisionNames = await _db.Divisions.ToDictionaryAsync(d => d.Id, d => d.Name);
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return RedirectToPage();
        u.IsActive = !u.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}
