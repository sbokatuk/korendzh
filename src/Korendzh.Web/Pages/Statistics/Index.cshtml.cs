using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Korendzh.Infrastructure.Services;
using Korendzh.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Statistics;

[Authorize(Policy = Auth.AuthorizationPolicies.ManagerOrAdmin)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IStatisticsService _stats;

    public IndexModel(AppDbContext db, UserManager<AppUser> users, IStatisticsService stats)
    {
        _db = db;
        _users = users;
        _stats = stats;
    }

    [BindProperty(SupportsGet = true)] public string By { get; set; } = "worker";
    [BindProperty(SupportsGet = true)] public DateOnly From { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
    [BindProperty(SupportsGet = true)] public DateOnly To { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty(SupportsGet = true)] public Guid? DivisionId { get; set; }

    public List<StatBucket> Buckets { get; private set; } = new();
    public List<Division> Divisions { get; private set; } = new();
    public bool IsAdmin { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();
        IsAdmin = await _users.IsInRoleAsync(actor, Roles.Admin);

        var divisionId = IsAdmin ? DivisionId : actor.DivisionId;
        Buckets = await BuildAsync(By, divisionId);

        if (IsAdmin) Divisions = await _db.Divisions.OrderBy(d => d.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetCsvAsync()
    {
        var (buckets, title) = await PrepareExportAsync();
        var bytes = CsvExporter.Export(buckets, title);
        return File(bytes, "text/csv; charset=utf-8", $"stats-{By}-{From:yyyyMMdd}-{To:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> OnGetXlsxAsync()
    {
        var (buckets, title) = await PrepareExportAsync();
        var bytes = XlsxExporter.Export(buckets, title);
        return File(bytes, XlsxExporter.ContentType, $"stats-{By}-{From:yyyyMMdd}-{To:yyyyMMdd}.xlsx");
    }

    private async Task<(List<StatBucket>, string)> PrepareExportAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) throw new UnauthorizedAccessException();
        IsAdmin = await _users.IsInRoleAsync(actor, Roles.Admin);
        var divisionId = IsAdmin ? DivisionId : actor.DivisionId;
        var buckets = await BuildAsync(By, divisionId);
        var title = $"Статистика: {ByLabel()} за {From:yyyy-MM-dd} — {To:yyyy-MM-dd}";
        return (buckets, title);
    }

    private string ByLabel() => By switch
    {
        "task" => "часы по задачам",
        "car" => "часы по автомобилям",
        _ => "часы по воркерам"
    };

    private async Task<List<StatBucket>> BuildAsync(string by, Guid? divisionId)
    {
        return by switch
        {
            "task" => await _stats.HoursByTaskAsync(divisionId, From, To),
            "car" => await _stats.HoursByCarAsync(divisionId, From, To),
            _ => await _stats.HoursByWorkerAsync(divisionId, From, To),
        };
    }
}
