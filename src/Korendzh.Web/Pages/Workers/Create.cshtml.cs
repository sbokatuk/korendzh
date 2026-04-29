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

namespace Korendzh.Web.Pages.Workers;

[Authorize(Policy = Auth.AuthorizationPolicies.ManagerOrAdmin)]
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

    public bool IsAdmin { get; private set; }
    public List<Division> Divisions { get; private set; } = new();

    public class InputModel
    {
        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        public Guid? DivisionId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();
        IsAdmin = await _users.IsInRoleAsync(actor, Roles.Admin);
        if (IsAdmin) Divisions = await _db.Divisions.Where(d => !d.IsArchived).OrderBy(d => d.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        IsAdmin = await _users.IsInRoleAsync(actor, Roles.Admin);
        if (IsAdmin) Divisions = await _db.Divisions.Where(d => !d.IsArchived).OrderBy(d => d.Name).ToListAsync();

        Guid? divisionId = IsAdmin ? Input.DivisionId : actor.DivisionId;
        if (!divisionId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.DivisionId), "Подразделение обязательно.");
        }
        if (!ModelState.IsValid) return Page();

        try
        {
            await _invites.CreateInviteAsync(Input.Email.Trim(), Input.FullName.Trim(), Roles.Worker, divisionId, actor.Id);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }

        TempData["StatusMessage"] = "Приглашение отправлено.";
        return RedirectToPage("/Workers/Index");
    }
}
