using System.ComponentModel.DataAnnotations;
using Korendzh.Domain;
using Korendzh.Domain.Plan;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Korendzh.Infrastructure.Plan;
using Korendzh.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Plan;

[Authorize]
public class BulkModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IPlanService _plan;
    private readonly DivisionScope _scope;

    public BulkModel(AppDbContext db, UserManager<AppUser> users, IPlanService plan, DivisionScope scope)
    {
        _db = db;
        _users = users;
        _plan = plan;
        _scope = scope;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public List<Guid> WorkerIds { get; set; } = new();

    public List<AppUser> AvailableWorkers { get; private set; } = new();
    public bool IsManagerOrAdmin { get; private set; }
    public Guid? PreselectedWorkerId { get; private set; }

    public class InputModel
    {
        [Required, DataType(DataType.Date)] public DateOnly From { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        [Required, DataType(DataType.Date)] public DateOnly To { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(1));
        [Required] public SchedulePattern Pattern { get; set; } = SchedulePattern.StandardWeek;
        [Range(0.25, 24)] public decimal HoursPerDay { get; set; } = 8m;
        public bool ReplaceExisting { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(Guid? workerId = null)
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        await LoadAvailableWorkers(actor);
        PreselectedWorkerId = workerId ?? actor.Id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        await LoadAvailableWorkers(actor);

        if (!ModelState.IsValid) return Page();
        if (Input.To < Input.From)
        {
            ModelState.AddModelError(string.Empty, "Дата 'до' должна быть не раньше 'от'.");
            return Page();
        }
        if (WorkerIds.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Выберите хотя бы одного работника.");
            return Page();
        }

        // Проверяем права на каждого выбранного воркера.
        foreach (var id in WorkerIds)
        {
            if (id != actor.Id && !await _scope.CanAccessWorkerAsync(actor, id))
            {
                return Forbid();
            }
        }

        try
        {
            await _plan.BulkFillAsync(WorkerIds, Input.From, Input.To, Input.Pattern,
                Input.HoursPerDay, Input.ReplaceExisting, actor.Id);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }

        TempData["StatusMessage"] = $"План применён для {WorkerIds.Count} работник(ов) на {(Input.To.DayNumber - Input.From.DayNumber + 1)} дн.";
        // Если выбран один воркер — отправляем на его план; иначе обратно к списку.
        if (WorkerIds.Count == 1)
            return RedirectToPage("Index", new { workerId = WorkerIds[0] });
        return RedirectToPage("Workers");
    }

    private async Task LoadAvailableWorkers(AppUser actor)
    {
        IsManagerOrAdmin = await _users.IsInRoleAsync(actor, Roles.Admin)
                          || await _users.IsInRoleAsync(actor, Roles.Manager);

        if (await _users.IsInRoleAsync(actor, Roles.Admin))
        {
            AvailableWorkers = await _db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        }
        else if (await _users.IsInRoleAsync(actor, Roles.Manager) && actor.DivisionId.HasValue)
        {
            AvailableWorkers = await _db.Users
                .Where(u => u.IsActive && u.DivisionId == actor.DivisionId)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }
        else
        {
            // Воркер видит только себя.
            AvailableWorkers = new List<AppUser> { actor };
        }
    }
}
