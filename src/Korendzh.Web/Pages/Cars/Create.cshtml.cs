using System.ComponentModel.DataAnnotations;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages.Cars;

[Authorize(Policy = Auth.AuthorizationPolicies.ManagerOrAdmin)]
public class CreateModel : PageModel
{
    private readonly UserManager<AppUser> _users;
    private readonly ICarService _cars;

    public CreateModel(UserManager<AppUser> users, ICarService cars)
    {
        _users = users;
        _cars = cars;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        [MaxLength(20)] public string? LicensePlate { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();
        if (!ModelState.IsValid) return Page();

        await _cars.GetOrCreateAsync(Input.Name.Trim(), Input.LicensePlate?.Trim(), actor.Id);
        TempData["StatusMessage"] = "Автомобиль добавлен.";
        return RedirectToPage("/Cars/Index");
    }
}
