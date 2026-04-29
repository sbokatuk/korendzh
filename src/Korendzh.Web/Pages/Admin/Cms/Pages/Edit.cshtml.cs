using System.ComponentModel.DataAnnotations;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CmsPage = Korendzh.Domain.Cms.Page;

namespace Korendzh.Web.Pages.Admin.Cms.Pages;

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

    [BindProperty] public InputModel Input { get; set; } = new();

    public CmsPage? Target { get; private set; }

    public class InputModel
    {
        public Guid Id { get; set; }
        [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
        [Required, MaxLength(100), RegularExpression("^[a-z0-9-]+$")] public string Slug { get; set; } = string.Empty;
        public string ContentHtml { get; set; } = string.Empty;
        public bool ShowInMenu { get; set; }
        public int MenuOrder { get; set; } = 100;
        public bool IsPublished { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Target = await _db.Pages.FirstOrDefaultAsync(p => p.Id == id);
        if (Target is null) return Page();
        Input = new InputModel
        {
            Id = Target.Id,
            Title = Target.Title,
            Slug = Target.Slug,
            ContentHtml = Target.ContentHtml,
            ShowInMenu = Target.ShowInMenu,
            MenuOrder = Target.MenuOrder,
            IsPublished = Target.IsPublished,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Target = await _db.Pages.FirstOrDefaultAsync(p => p.Id == Input.Id);
        if (Target is null) return Page();
        if (!ModelState.IsValid) return Page();

        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        Target.Title = Input.Title.Trim();
        Target.Slug = Input.Slug.Trim();
        Target.ContentHtml = Input.ContentHtml;
        Target.ShowInMenu = Input.ShowInMenu;
        Target.MenuOrder = Input.MenuOrder;
        Target.IsPublished = Input.IsPublished;
        Target.UpdatedAt = DateTime.UtcNow;
        Target.UpdatedById = actor.Id;

        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Сохранено.";
        return RedirectToPage("Index");
    }
}
