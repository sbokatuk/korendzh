using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Admin.Cms.Reviews;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<Review> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await _db.Reviews
            .OrderBy(r => r.DisplayOrder).ThenByDescending(r => r.ReviewDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var r = await _db.Reviews.FirstOrDefaultAsync(x => x.Id == id);
        if (r is not null) { _db.Reviews.Remove(r); await _db.SaveChangesAsync(); TempData["StatusMessage"] = "Отзыв удалён."; }
        return RedirectToPage();
    }
}
