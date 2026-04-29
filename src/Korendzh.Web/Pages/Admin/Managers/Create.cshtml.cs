using System.ComponentModel.DataAnnotations;
using Korendzh.Domain;
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
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IInviteService _invites;

    public CreateModel(AppDbContext db, UserManager<AppUser> users, IInviteService invites)
    {
        _db = db;
        _users = users;
        _invites = invites;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<Division> Divisions { get; private set; } = new();

    public class InputModel
    {
        [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = string.Empty;
        [Required] public Guid DivisionId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Divisions = await _db.Divisions.Where(d => !d.IsArchived).OrderBy(d => d.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Divisions = await _db.Divisions.Where(d => !d.IsArchived).OrderBy(d => d.Name).ToListAsync();
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();
        if (!ModelState.IsValid) return Page();

        try
        {
            var manager = await _invites.CreateInviteAsync(Input.Email.Trim(), Input.FullName.Trim(),
                Roles.Manager, Input.DivisionId, actor.Id);

            // Привязываем подразделение к этому менеджеру.
            var division = await _db.Divisions.FirstOrDefaultAsync(d => d.Id == Input.DivisionId);
            if (division is not null)
            {
                division.ManagerId = manager.Id;
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }

        TempData["StatusMessage"] = "Приглашение отправлено.";
        return RedirectToPage("/Admin/Managers/Index");
    }
}
