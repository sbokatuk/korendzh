using System.Globalization;
using Korendzh.Domain;
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
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IPlanService _plan;
    private readonly DivisionScope _scope;

    public IndexModel(AppDbContext db, UserManager<AppUser> users, IPlanService plan, DivisionScope scope)
    {
        _db = db;
        _users = users;
        _plan = plan;
        _scope = scope;
    }

    public Guid WorkerId { get; private set; }
    public string WorkerName { get; private set; } = string.Empty;
    public DateOnly MonthStart { get; private set; }
    public List<DayCell> Days { get; private set; } = new();
    public decimal TotalPlannedHours => Days.Sum(d => d.Hours);
    public bool IsManagerOrAdminView { get; private set; }

    public record DayCell(DateOnly Date, decimal Hours);

    public async Task<IActionResult> OnGetAsync(Guid? workerId = null, string? month = null)
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        WorkerId = workerId ?? actor.Id;
        if (WorkerId != actor.Id && !await _scope.CanAccessWorkerAsync(actor, WorkerId))
            return Forbid();

        IsManagerOrAdminView = WorkerId != actor.Id;

        var worker = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == WorkerId);
        if (worker is null) return NotFound();
        WorkerName = worker.FullName;

        MonthStart = ParseMonth(month) ?? new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var monthEnd = MonthStart.AddMonths(1).AddDays(-1);

        var entries = await _plan.GetPlanAsync(WorkerId, MonthStart, monthEnd);
        var byDate = entries.ToDictionary(e => e.WorkDate, e => e.PlannedHours);

        Days = new List<DayCell>();
        for (var d = MonthStart; d <= monthEnd; d = d.AddDays(1))
        {
            byDate.TryGetValue(d, out var h);
            Days.Add(new DayCell(d, h));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid WorkerId, string MonthStart, [FromForm] List<string> Dates, [FromForm] List<string> Hours)
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        if (WorkerId != actor.Id && !await _scope.CanAccessWorkerAsync(actor, WorkerId))
            return Forbid();

        var batch = new Dictionary<DateOnly, decimal>();
        for (int i = 0; i < Dates.Count && i < Hours.Count; i++)
        {
            if (!DateOnly.TryParse(Dates[i], out var date)) continue;
            decimal hours = 0m;
            if (!string.IsNullOrWhiteSpace(Hours[i]))
            {
                if (!decimal.TryParse(Hours[i].Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out hours)) continue;
                if (hours < 0) hours = 0;
                if (hours > 24) hours = 24;
            }
            batch[date] = hours;
        }

        await _plan.SaveBatchAsync(WorkerId, batch, actor.Id);
        TempData["StatusMessage"] = "План сохранён.";

        var monthQuery = MonthStart.Substring(0, 7); // "yyyy-MM"
        return RedirectToPage(new { workerId = WorkerId, month = monthQuery });
    }

    private static DateOnly? ParseMonth(string? month)
    {
        if (string.IsNullOrWhiteSpace(month)) return null;
        if (DateOnly.TryParseExact(month, "yyyy-MM-dd", out var d1)) return new DateOnly(d1.Year, d1.Month, 1);
        if (DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out var d2)) return d2;
        return null;
    }
}
