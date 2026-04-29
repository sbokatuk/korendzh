using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Korendzh.Infrastructure.Services;
using Korendzh.Web.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.TimeEntries;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly ITimeEntryService _entries;
    private readonly DivisionScope _scope;

    public DeleteModel(AppDbContext db, UserManager<AppUser> users, ITimeEntryService entries, DivisionScope scope)
    {
        _db = db;
        _users = users;
        _entries = entries;
        _scope = scope;
    }

    public TimeEntry? Entry { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();
        Entry = await _db.TimeEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (Entry is null) return Page();
        if (!await _scope.CanAccessTimeEntryAsync(actor, Entry.WorkerId)) return Forbid();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        Entry = await _db.TimeEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (Entry is null) return RedirectToPage("/TimeEntries/Index");
        if (!await _scope.CanAccessTimeEntryAsync(actor, Entry.WorkerId)) return Forbid();

        await _entries.SoftDeleteAsync(id, actor.Id);
        TempData["StatusMessage"] = "Запись удалена.";
        return RedirectToPage("/TimeEntries/Index");
    }
}
