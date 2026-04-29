using System.ComponentModel.DataAnnotations;
using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Korendzh.Infrastructure.Services;
using Korendzh.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Controllers;

[ApiController]
[Route("api/timeentries")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class TimeEntriesApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly ITimeEntryService _entries;
    private readonly ICarService _cars;
    private readonly DivisionScope _scope;

    public TimeEntriesApiController(
        AppDbContext db,
        UserManager<AppUser> users,
        ITimeEntryService entries,
        ICarService cars,
        DivisionScope scope)
    {
        _db = db;
        _users = users;
        _entries = entries;
        _cars = cars;
        _scope = scope;
    }

    public record TimeEntryDto(
        Guid Id, Guid WorkerId, DateOnly WorkDate, decimal Hours,
        string TaskName, Guid? CarId, string? CarName, string? LicensePlate, string? Description,
        DateTime CreatedAt, DateTime? UpdatedAt);

    public record CreateRequest(
        Guid? WorkerId,
        [Required] DateOnly WorkDate,
        [Range(0.01, 24.0)] decimal Hours,
        [Required, MaxLength(200)] string TaskName,
        [MaxLength(100)] string? CarName,
        [MaxLength(20)] string? LicensePlate,
        [MaxLength(500)] string? Description);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? workerId, CancellationToken ct)
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Unauthorized();

        var f = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var t = to ?? DateOnly.FromDateTime(DateTime.Today);

        IQueryable<TimeEntry> q = _db.TimeEntries.Where(e => e.WorkDate >= f && e.WorkDate <= t);

        var isAdmin = await _users.IsInRoleAsync(actor, Roles.Admin);
        var isManager = await _users.IsInRoleAsync(actor, Roles.Manager);

        if (isAdmin)
        {
            if (workerId.HasValue) q = q.Where(e => e.WorkerId == workerId);
        }
        else if (isManager)
        {
            var ids = await _db.Users.Where(u => u.DivisionId == actor.DivisionId).Select(u => u.Id).ToListAsync(ct);
            q = q.Where(e => ids.Contains(e.WorkerId));
            if (workerId.HasValue) q = q.Where(e => e.WorkerId == workerId);
        }
        else
        {
            q = q.Where(e => e.WorkerId == actor.Id);
        }

        var rows = await q.OrderByDescending(e => e.WorkDate).ToListAsync(ct);
        var carNames = await _db.Cars.Where(c => rows.Select(r => r.CarId).Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var result = rows.Select(r => new TimeEntryDto(
            r.Id, r.WorkerId, r.WorkDate, r.Hours, r.TaskName, r.CarId,
            r.CarId.HasValue ? carNames.GetValueOrDefault(r.CarId.Value) : null,
            r.LicensePlate, r.Description, r.CreatedAt, r.UpdatedAt));

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken ct)
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Unauthorized();

        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        // Дата работы не ограничена будущим — табель может оформляться авансом.
        if (!string.IsNullOrWhiteSpace(req.CarName) ^ !string.IsNullOrWhiteSpace(req.LicensePlate))
            return BadRequest(new { error = "car_fields_inconsistent" });

        var workerId = req.WorkerId ?? actor.Id;
        if (!await _scope.CanAccessWorkerAsync(actor, workerId)) return Forbid();

        Guid? carId = null;
        if (!string.IsNullOrWhiteSpace(req.CarName))
        {
            var car = await _cars.GetOrCreateAsync(req.CarName!, req.LicensePlate, actor.Id, ct);
            carId = car.Id;
        }

        var entry = new TimeEntry
        {
            WorkerId = workerId,
            WorkDate = req.WorkDate,
            Hours = req.Hours,
            TaskName = req.TaskName.Trim(),
            CarId = carId,
            LicensePlate = req.LicensePlate?.Trim(),
            Description = req.Description?.Trim(),
            CreatedById = actor.Id,
        };

        await _entries.CreateAsync(entry, actor.Id, ct);
        return CreatedAtAction(nameof(List), new { }, new { id = entry.Id });
    }
}
