using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Korendzh.Infrastructure.Plan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Statistics;

[Authorize(Policy = Auth.AuthorizationPolicies.ManagerOrAdmin)]
public class PlanModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IPlanService _plan;

    public PlanModel(AppDbContext db, UserManager<AppUser> users, IPlanService plan)
    {
        _db = db;
        _users = users;
        _plan = plan;
    }

    [BindProperty(SupportsGet = true)] public DateOnly From { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
    [BindProperty(SupportsGet = true)] public DateOnly To { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty(SupportsGet = true)] public Guid? DivisionId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? WorkerId { get; set; }
    [BindProperty(SupportsGet = true)] public PlanGranularity Granularity { get; set; } = PlanGranularity.Day;

    public List<PlanBucket> Buckets { get; private set; } = new();
    public List<Domain.Division> Divisions { get; private set; } = new();
    public List<AppUser> WorkerOptions { get; private set; } = new();
    public bool IsAdmin { get; private set; }

    public decimal TotalPlanned => Buckets.Sum(b => b.PlannedHours);
    public decimal TotalActual => Buckets.Sum(b => b.ActualHours);

    public async Task<IActionResult> OnGetAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        IsAdmin = await _users.IsInRoleAsync(actor, Roles.Admin);

        // Менеджер ограничен своим подразделением.
        Guid? effectiveDivision = IsAdmin ? DivisionId : actor.DivisionId;

        // Если выбран воркер — проверим, что он попадает в видимое подразделение.
        if (WorkerId.HasValue)
        {
            var worker = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == WorkerId);
            if (worker is null) return NotFound();
            if (!IsAdmin && worker.DivisionId != actor.DivisionId) return Forbid();
        }

        // Сначала суточные данные, потом — группировка по выбранной гранулярности.
        var daily = await _plan.GetPlanVsActualAsync(effectiveDivision, WorkerId, From, To);
        Buckets = daily.GroupByGranularity(Granularity, anchor: From);

        if (IsAdmin)
        {
            Divisions = await _db.Divisions.OrderBy(d => d.Name).ToListAsync();
            WorkerOptions = effectiveDivision.HasValue
                ? await _db.Users.Where(u => u.DivisionId == effectiveDivision).OrderBy(u => u.FullName).ToListAsync()
                : await _db.Users.OrderBy(u => u.FullName).ToListAsync();
        }
        else
        {
            WorkerOptions = await _db.Users
                .Where(u => u.DivisionId == actor.DivisionId)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        return Page();
    }
}
