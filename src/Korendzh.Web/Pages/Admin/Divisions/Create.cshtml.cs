using System.ComponentModel.DataAnnotations;
using Korendzh.Domain;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Admin.Divisions;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var name = Input.Name.Trim();
        if (await _db.Divisions.AnyAsync(d => d.Name == name))
        {
            ModelState.AddModelError(nameof(Input.Name), "Подразделение с таким названием уже существует.");
            return Page();
        }

        _db.Divisions.Add(new Division { Name = name });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Подразделение создано.";
        return RedirectToPage("/Admin/Divisions/Index");
    }
}
