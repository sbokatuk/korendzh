using System.ComponentModel.DataAnnotations;
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

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly ITimeEntryService _entries;
    private readonly ICarService _cars;
    private readonly DivisionScope _scope;

    public EditModel(AppDbContext db, UserManager<AppUser> users, ITimeEntryService entries, ICarService cars, DivisionScope scope)
    {
        _db = db;
        _users = users;
        _entries = entries;
        _cars = cars;
        _scope = scope;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool EntryNotFound { get; set; }

    public class InputModel
    {
        public Guid Id { get; set; }
        public byte[]? RowVersion { get; set; }

        [Required, DataType(DataType.Date)]
        public DateOnly WorkDate { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Часы должны быть больше нуля")]
        public decimal Hours { get; set; }

        [Required, MaxLength(200)]
        public string TaskName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? CarName { get; set; }

        [MaxLength(20)]
        public string? LicensePlate { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        var entry = await _db.TimeEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (entry is null)
        {
            EntryNotFound = true;
            return Page();
        }
        if (!await _scope.CanAccessTimeEntryAsync(actor, entry.WorkerId)) return Forbid();

        var carName = entry.CarId.HasValue
            ? (await _db.Cars.Where(c => c.Id == entry.CarId).Select(c => c.Name).FirstOrDefaultAsync())
            : null;

        Input = new InputModel
        {
            Id = entry.Id,
            RowVersion = entry.RowVersion,
            WorkDate = entry.WorkDate,
            Hours = entry.Hours,
            TaskName = entry.TaskName,
            CarName = carName,
            LicensePlate = entry.LicensePlate,
            Description = entry.Description,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        var entry = await _db.TimeEntries.FirstOrDefaultAsync(e => e.Id == Input.Id);
        if (entry is null)
        {
            EntryNotFound = true;
            return Page();
        }
        if (!await _scope.CanAccessTimeEntryAsync(actor, entry.WorkerId)) return Forbid();

        // Дата работы не ограничена будущим — табель может оформляться авансом.
        // XOR-валидация «авто+номер вместе или ничего» убрана: разрешаем любое сочетание.

        if (!ModelState.IsValid) return Page();

        var plate = string.IsNullOrWhiteSpace(Input.LicensePlate) ? null : Input.LicensePlate!.Trim();
        var carName = string.IsNullOrWhiteSpace(Input.CarName) ? null : Input.CarName!.Trim();
        if (carName is null && plate is not null) carName = plate;

        Guid? carId = null;
        if (carName is not null)
        {
            var car = await _cars.GetOrCreateAsync(carName, plate, actor.Id);
            carId = car.Id;
        }

        // Optimistic concurrency: установим original RowVersion и дадим EF самому проверить.
        if (Input.RowVersion is not null)
        {
            _db.Entry(entry).Property(e => e.RowVersion).OriginalValue = Input.RowVersion;
        }

        var update = new TimeEntry
        {
            Id = entry.Id,
            WorkerId = entry.WorkerId,
            WorkDate = Input.WorkDate,
            Hours = Input.Hours,
            TaskName = Input.TaskName.Trim(),
            CarId = carId,
            LicensePlate = plate,
            Description = Input.Description?.Trim(),
        };

        try
        {
            await _entries.UpdateAsync(update, actor.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Запись была изменена кем-то другим. Обновите страницу и попробуйте снова.");
            return Page();
        }

        TempData["StatusMessage"] = "Запись обновлена.";
        return RedirectToPage("/TimeEntries/Index");
    }
}
