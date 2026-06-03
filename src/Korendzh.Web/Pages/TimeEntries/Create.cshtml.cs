using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
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
    private readonly ILogger<CreateModel> _log;

    public CreateModel(
        AppDbContext db,
        UserManager<AppUser> users,
        ITimeEntryService entries,
        ICarService cars,
        DivisionScope scope,
        ILogger<CreateModel> log)
    {
        _db = db;
        _users = users;
        _entries = entries;
        _cars = cars;
        _scope = scope;
        _log = log;
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
            _log.LogWarning("TimeEntry create denied: actor {ActorId} cannot access worker {WorkerId}", actor.Id, workerId);
            return Forbid();
        }

        // Парсим часы. Принимаем оба разделителя ('.' / ','), терпим лишние пробелы (включая NBSP)
        // и подсказки вроде «8ч», «8h», «8 hr». Серверный парсер — источник истины, JS лишь подсказывает.
        decimal hours = 0m;
        var hoursParsed = TryParseHours(Input.Hours, out hours);
        if (!hoursParsed)
        {
            ModelState.AddModelError(nameof(Input.Hours),
                "Введите количество часов больше нуля (например, 1.5 или 1,5). Верхнего предела нет.");
        }

        // XOR-валидации «авто+номер вместе или ничего» больше нет: разрешаем любое сочетание.
        // - оба пусты → CarId = null;
        // - только номер → используем его и как название (чтобы строка попала в справочник);
        // - только название → сохраняем без номера;
        // - оба есть → стандартный кейс.

        if (!ModelState.IsValid)
        {
            _log.LogInformation(
                "TimeEntry create: validation failed. HoursRaw='{HoursRaw}', CarName='{Car}', Plate='{Plate}', Errors={Errors}",
                Input.Hours, Input.CarName, Input.LicensePlate,
                string.Join("; ", ModelState
                    .SelectMany(kv => kv.Value!.Errors.Select(e => $"{kv.Key}: {e.ErrorMessage}"))));
            return Page();
        }

        var plate = string.IsNullOrWhiteSpace(Input.LicensePlate) ? null : Input.LicensePlate!.Trim();
        var carName = string.IsNullOrWhiteSpace(Input.CarName) ? null : Input.CarName!.Trim();
        if (carName is null && plate is not null) carName = plate;

        Guid? carId = null;
        try
        {
            if (carName is not null)
            {
                var car = await _cars.GetOrCreateAsync(carName, plate, actor.Id);
                carId = car.Id;
            }

            var entry = new TimeEntry
            {
                WorkerId = workerId,
                WorkDate = Input.WorkDate == default ? DateOnly.FromDateTime(DateTime.Today) : Input.WorkDate,
                Hours = hours,
                TaskName = (Input.TaskName ?? string.Empty).Trim(),
                CarId = carId,
                LicensePlate = plate,
                Description = Input.Description?.Trim(),
                CreatedById = actor.Id,
            };

            _log.LogInformation(
                "TimeEntry create: actor={ActorId} worker={WorkerId} date={Date} hours={Hours} car='{Car}' plate='{Plate}'",
                actor.Id, workerId, entry.WorkDate, entry.Hours, carName, plate);

            await _entries.CreateAsync(entry, actor.Id);

            _log.LogInformation("TimeEntry create: success id={EntryId}", entry.Id);
            TempData["StatusMessage"] = "Запись сохранена.";
            return RedirectToPage("/TimeEntries/Index");
        }
        catch (Exception ex)
        {
            _log.LogError(ex,
                "TimeEntry create FAILED. actor={ActorId} worker={WorkerId} hours={Hours} carName='{Car}' plate='{Plate}'",
                actor.Id, workerId, hours, carName, plate);
            ModelState.AddModelError(string.Empty,
                "Не получилось сохранить запись. Попробуйте ещё раз, а если повторится — сообщите мастеру.");
            return Page();
        }
    }

    /// <summary>
    /// Мягкий парсер часов. Принимает '1.5', '1,5', '1 ч', '8h', '0.25hr', с NBSP и без.
    /// Логика: вырезаем NBSP, нормализуем запятую в точку, оставляем только цифры и одну точку,
    /// пытаемся распарсить как decimal в инвариантной культуре. Результат должен быть > 0 (верхнего предела нет).
    /// </summary>
    internal static bool TryParseHours(string? raw, out decimal hours)
    {
        hours = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        // Запятая → точка; всё остальное (буквы, пробелы любого вида, лишние разделители) выкидываем в цикле ниже.
        var normalized = raw.Replace(',', '.');

        var sb = new StringBuilder(normalized.Length);
        bool dotSeen = false;
        foreach (var ch in normalized)
        {
            if (char.IsDigit(ch))
            {
                sb.Append(ch);
            }
            else if (ch == '.' && !dotSeen)
            {
                sb.Append(ch);
                dotSeen = true;
            }
            // всё остальное (буквы «ч»/«h»/«hr», пробелы, лишние разделители) — игнорируем
        }

        var clean = sb.ToString();
        if (clean.Length == 0 || clean == ".") return false;

        if (!decimal.TryParse(clean, NumberStyles.Number, CultureInfo.InvariantCulture, out hours)) return false;
        if (hours <= 0m) return false; // верхнего предела нет, см. docs/validation.md
        return true;
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
