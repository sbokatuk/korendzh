using System.ComponentModel.DataAnnotations;
using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Workers;

[Authorize(Policy = Auth.AuthorizationPolicies.ManagerOrAdmin)]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;

    public EditModel(AppDbContext db, UserManager<AppUser> users)
    {
        _db = db;
        _users = users;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public AppUser? Target { get; private set; }
    public bool IsAdmin { get; private set; }
    public List<Division> Divisions { get; private set; } = new();

    public class InputModel
    {
        public Guid Id { get; set; }
        [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = string.Empty;
        public Guid? DivisionId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();
        Target = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (Target is null) return Page();

        IsAdmin = await _users.IsInRoleAsync(actor, Roles.Admin);
        if (!IsAdmin && Target.DivisionId != actor.DivisionId) return Forbid();

        if (IsAdmin) Divisions = await _db.Divisions.OrderBy(d => d.Name).ToListAsync();

        Input = new InputModel
        {
            Id = Target.Id,
            FullName = Target.FullName,
            Email = Target.Email ?? string.Empty,
            DivisionId = Target.DivisionId,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();
        Target = await _db.Users.FirstOrDefaultAsync(u => u.Id == Input.Id);
        if (Target is null) return Page();

        IsAdmin = await _users.IsInRoleAsync(actor, Roles.Admin);
        if (!IsAdmin && Target.DivisionId != actor.DivisionId) return Forbid();
        if (IsAdmin) Divisions = await _db.Divisions.OrderBy(d => d.Name).ToListAsync();

        if (!ModelState.IsValid) return Page();

        Target.FullName = Input.FullName.Trim();
        Target.Email = Input.Email.Trim();
        Target.UserName = Input.Email.Trim();
        if (IsAdmin) Target.DivisionId = Input.DivisionId;

        var update = await _users.UpdateAsync(Target);
        if (!update.Succeeded)
        {
            foreach (var e in update.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return Page();
        }

        TempData["StatusMessage"] = "Сохранено.";
        return RedirectToPage("/Workers/Index");
    }
}
