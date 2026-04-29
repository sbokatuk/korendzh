using Korendzh.Domain;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Admin.Divisions;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Division> Items { get; private set; } = new();
    public Dictionary<Guid, string> ManagerNames { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await _db.Divisions.OrderBy(d => d.Name).ToListAsync();
        var managerIds = Items.Where(d => d.ManagerId.HasValue).Select(d => d.ManagerId!.Value).Distinct().ToList();
        ManagerNames = await _db.Users.Where(u => managerIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName);
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id)
    {
        var d = await _db.Divisions.FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return RedirectToPage();
        d.IsArchived = !d.IsArchived;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}
