using System.ComponentModel.DataAnnotations;
using Korendzh.Domain;
using Korendzh.Domain.Cms; // not used directly but kept for symmetry
using Korendzh.Infrastructure.Auth;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Admin.Managers;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
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
        Target = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (Target is null) return Page();

        // Допускаем редактирование только тех, кто реально менеджер.
        if (!await _users.IsInRoleAsync(Target, Roles.Manager))
        {
            Target = null;
            return Page();
        }

        Divisions = await _db.Divisions.OrderBy(d => d.Name).ToListAsync();

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
        Target = await _db.Users.FirstOrDefaultAsync(u => u.Id == Input.Id);
        if (Target is null) return Page();
        if (!await _users.IsInRoleAsync(Target, Roles.Manager)) { Target = null; return Page(); }

        Divisions = await _db.Divisions.OrderBy(d => d.Name).ToListAsync();
        if (!ModelState.IsValid) return Page();

        // Email всегда нормализуем к ASCII (см. EmailNormalizer).
        var newEmail = EmailNormalizer.ToAscii(Input.Email.Trim());
        Target.FullName = Input.FullName.Trim();
        Target.Email = newEmail;
        Target.UserName = newEmail;
        Target.DivisionId = Input.DivisionId;

        await _users.UpdateNormalizedEmailAsync(Target);
        await _users.UpdateNormalizedUserNameAsync(Target);
        var update = await _users.UpdateAsync(Target);
        if (!update.Succeeded)
        {
            foreach (var e in update.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return Page();
        }

        // Перепривязываем подразделение: новый менеджер становится владельцем,
        // прежний владелец того же подразделения сбрасывается.
        if (Input.DivisionId.HasValue)
        {
            var div = await _db.Divisions.FirstOrDefaultAsync(d => d.Id == Input.DivisionId);
            if (div is not null && div.ManagerId != Target.Id)
            {
                div.ManagerId = Target.Id;
                await _db.SaveChangesAsync();
            }
        }

        TempData["StatusMessage"] = "Сохранено.";
        return RedirectToPage("Index");
    }
}
