using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Admin.Cms.Services;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<Service> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await _db.Services.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Title).ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var s = await _db.Services.FirstOrDefaultAsync(x => x.Id == id);
        if (s is not null)
        {
            _db.Services.Remove(s);
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Услуга удалена.";
        }
        return RedirectToPage();
    }
}
