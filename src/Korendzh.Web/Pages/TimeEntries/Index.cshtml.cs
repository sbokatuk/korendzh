using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.TimeEntries;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;

    public IndexModel(AppDbContext db, UserManager<AppUser> users)
    {
        _db = db;
        _users = users;
    }

    [BindProperty(SupportsGet = true)] public DateOnly From { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
    [BindProperty(SupportsGet = true)] public DateOnly To { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty(SupportsGet = true)] public Guid? WorkerId { get; set; }

    public List<TimeEntry> Entries { get; private set; } = new();
    public List<AppUser> Workers { get; private set; } = new();
    public Dictionary<Guid, string> WorkerNames { get; private set; } = new();
    public Dictionary<Guid, string> CarNames { get; private set; } = new();

    public bool IsManagerOrAdmin { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        var isAdmin = await _users.IsInRoleAsync(actor, Roles.Admin);
        var isManager = await _users.IsInRoleAsync(actor, Roles.Manager);
        IsManagerOrAdmin = isAdmin || isManager;

        IQueryable<TimeEntry> q = _db.TimeEntries.Where(e => e.WorkDate >= From && e.WorkDate <= To);

        if (isAdmin)
        {
            // Видит всё.
        }
        else if (isManager)
        {
            // Только своё подразделение.
            var workerIds = await _db.Users.Where(u => u.DivisionId == actor.DivisionId).Select(u => u.Id).ToListAsync();
            q = q.Where(e => workerIds.Contains(e.WorkerId));
            Workers = await _db.Users
                .Where(u => u.DivisionId == actor.DivisionId)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }
        else
        {
            // Worker — только свои.
            q = q.Where(e => e.WorkerId == actor.Id);
        }

        if (IsManagerOrAdmin && WorkerId.HasValue)
        {
            q = q.Where(e => e.WorkerId == WorkerId.Value);
        }
        if (isAdmin)
        {
            Workers = await _db.Users.OrderBy(u => u.FullName).ToListAsync();
        }

        Entries = await q.OrderByDescending(e => e.WorkDate).ThenByDescending(e => e.CreatedAt).ToListAsync();

        var workerIdSet = Entries.Select(e => e.WorkerId).Distinct().ToList();
        WorkerNames = await _db.Users
            .Where(u => workerIdSet.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var carIds = Entries.Where(e => e.CarId.HasValue).Select(e => e.CarId!.Value).Distinct().ToList();
        CarNames = await _db.Cars.Where(c => carIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name);

        return Page();
    }
}
