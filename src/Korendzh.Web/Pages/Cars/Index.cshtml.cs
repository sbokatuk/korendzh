using Korendzh.Domain;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Cars;

[Authorize(Policy = Auth.AuthorizationPolicies.ManagerOrAdmin)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Car> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await _db.Cars.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id)
    {
        var c = await _db.Cars.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return RedirectToPage();
        c.IsActive = !c.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}
