using System.ComponentModel.DataAnnotations;
using Korendzh.Domain;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Cars;

[Authorize(Policy = Auth.AuthorizationPolicies.ManagerOrAdmin)]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Car? Target { get; private set; }

    public class InputModel
    {
        public Guid Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        [MaxLength(20)] public string? LicensePlate { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Target = await _db.Cars.FirstOrDefaultAsync(c => c.Id == id);
        if (Target is null) return Page();
        Input = new InputModel { Id = Target.Id, Name = Target.Name, LicensePlate = Target.LicensePlate };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Target = await _db.Cars.FirstOrDefaultAsync(c => c.Id == Input.Id);
        if (Target is null) return Page();
        if (!ModelState.IsValid) return Page();

        Target.Name = Input.Name.Trim();
        Target.LicensePlate = string.IsNullOrWhiteSpace(Input.LicensePlate) ? null : Input.LicensePlate.Trim();
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Сохранено.";
        return RedirectToPage("/Cars/Index");
    }
}
