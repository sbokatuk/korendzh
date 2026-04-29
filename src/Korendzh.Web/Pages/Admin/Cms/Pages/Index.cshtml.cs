using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CmsPage = Korendzh.Domain.Cms.Page;

namespace Korendzh.Web.Pages.Admin.Cms.Pages;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<CmsPage> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await _db.Pages.OrderBy(p => p.MenuOrder).ThenBy(p => p.Title).ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var p = await _db.Pages.FirstOrDefaultAsync(x => x.Id == id);
        if (p is not null) { _db.Pages.Remove(p); await _db.SaveChangesAsync(); TempData["StatusMessage"] = "Страница удалена."; }
        return RedirectToPage();
    }
}
