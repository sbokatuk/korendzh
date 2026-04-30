using System.ComponentModel.DataAnnotations;
using System.Globalization;
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

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly ITimeEntryService _entries;
    private readonly ICarService _cars;
    private readonly DivisionScope _scope;

    public CreateModel(AppDbContext db, UserManager<AppUser> users, ITimeEntryService entries, ICarService cars, DivisionScope scope)
    {
        _db = db;
        _users = users;
        _entries = entries;
        _cars = cars;
        _scope = scope;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new()
    {
        WorkDate = DateOnly.FromDateTime(DateTime.Today)
    };

    public bool IsManagerOrAdmin { get; private set; }
    public List<AppUser> Workers { get; private set; } = new();

    /// <summary>Последние 3 уникальных автомобиля из табелей текущего пользователя.</summary>
    public List<RecentCar> RecentCars { get; private set; } = new();
    public record RecentCar(string Name, string? LicensePlate);

    public class InputModel
    {
        public Guid? WorkerId { get; set; }

        // Дата по умолчанию = сегодня; пользователь может изменить, но поле не помечено [Required],
        // т.к. оно никогда не пустое (биндинг кладёт сегодняшнюю дату из инициализатора).
        [DataType(DataType.Date)]
        public DateOnly WorkDate { get; set; }

        /// <summary>
        /// Часы — единственное обязательное поле. Принимаем форматы '1.2' и '1,2',
        /// в БД храним как decimal в инвариантной форме.
        /// </summary>
        [Required(ErrorMessage = "Укажите количество часов")]
        public string Hours { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? TaskName { get; set; }

        [MaxLength(100)]
        public string? CarName { get; set; }

        [MaxLength(20)]
        public string? LicensePlate { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();
        await LoadWorkers(actor);
        if (!IsManagerOrAdmin) Input.WorkerId = actor.Id;
        await LoadRecentCars(actor.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        await LoadWorkers(actor);
        await LoadRecentCars(actor.Id);

        var workerId = Input.WorkerId ?? actor.Id;
        if (!await _scope.CanAccessWorkerAsync(actor, workerId))
        {
            return Forbid();
        }

        // Дата работы: не ограничиваем будущим — план/факт может оформляться авансом.

        // Парсим часы. Принимаем оба разделителя ('.' и ','), храним как decimal в БД.
        decimal hours = 0m;
        if (!string.IsNullOrWhiteSpace(Input.Hours))
        {
            var raw = Input.Hours.Trim().Replace(',', '.');
            if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out hours)
                || hours <= 0m || hours > 24m)
            {
                ModelState.AddModelError(nameof(Input.Hours),
                    "Введите количество часов от 0.01 до 24 (например, 1.5 или 1,5).");
            }
        }

        // Если CarName заполнено, либо LicensePlate тоже должен быть (см. validation.md).
        if (!string.IsNullOrWhiteSpace(Input.CarName) ^ !string.IsNullOrWhiteSpace(Input.LicensePlate))
        {
            ModelState.AddModelError(string.Empty, "Заполните оба поля автомобиля или оставьте оба пустыми.");
        }

        if (!ModelState.IsValid) return Page();

        Guid? carId = null;
        if (!string.IsNullOrWhiteSpace(Input.CarName))
        {
            var car = await _cars.GetOrCreateAsync(Input.CarName!, Input.LicensePlate, actor.Id);
            carId = car.Id;
        }

        var entry = new TimeEntry
        {
            WorkerId = workerId,
            WorkDate = Input.WorkDate == default ? DateOnly.FromDateTime(DateTime.Today) : Input.WorkDate,
            Hours = hours,
            TaskName = (Input.TaskName ?? string.Empty).Trim(),
            CarId = carId,
            LicensePlate = Input.LicensePlate?.Trim(),
            Description = Input.Description?.Trim(),
            CreatedById = actor.Id,
        };

        await _entries.CreateAsync(entry, actor.Id);
        TempData["StatusMessage"] = "Запись сохранена.";
        return RedirectToPage("/TimeEntries/Index");
    }

    private async Task LoadWorkers(AppUser actor)
    {
        var isAdmin = await _users.IsInRoleAsync(actor, Roles.Admin);
        var isManager = await _users.IsInRoleAsync(actor, Roles.Manager);
        IsManagerOrAdmin = isAdmin || isManager;

        if (isAdmin)
        {
            Workers = await _db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        }
        else if (isManager && actor.DivisionId.HasValue)
        {
            Workers = await _db.Users.Where(u => u.IsActive && u.DivisionId == actor.DivisionId).OrderBy(u => u.FullName).ToListAsync();
        }
    }

    /// <summary>
    /// Последние 3 уникальных автомобиля, использованных в табеле текущего пользователя.
    /// Берём 50 последних записей с авто, дедуплицируем по CarId, берём 3 свежих.
    /// </summary>
    private async Task LoadRecentCars(Guid actorId)
    {
        var recent = await _db.TimeEntries
            .AsNoTracking()
            .Where(e => e.WorkerId == actorId && e.CarId != null)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.CarId, e.LicensePlate })
            .Take(50)
            .ToListAsync();

        var distinct = recent
            .GroupBy(x => x.CarId!.Value)
            .Select(g => new { CarId = g.Key, LicensePlate = g.First().LicensePlate })
            .Take(3)
            .ToList();

        if (distinct.Count == 0) return;

        var carIds = distinct.Select(x => x.CarId).ToList();
        var carNames = await _db.Cars
            .AsNoTracking()
            .Where(c => carIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        RecentCars = distinct
            .Where(x => carNames.ContainsKey(x.CarId))
            .Select(x => new RecentCar(carNames[x.CarId], x.LicensePlate))
            .ToList();
    }
}
