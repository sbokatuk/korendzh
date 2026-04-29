using System.ComponentModel.DataAnnotations;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CmsPage = Korendzh.Domain.Cms.Page;

namespace Korendzh.Web.Pages.Admin.Cms.Pages;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;

    public CreateModel(AppDbContext db, UserManager<AppUser> users)
    {
        _db = db;
        _users = users;
    }

    [BindProperty] public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
        [Required, MaxLength(100), RegularExpression("^[a-z0-9-]+$")] public string Slug { get; set; } = string.Empty;
        public string ContentHtml { get; set; } = string.Empty;
        public bool ShowInMenu { get; set; }
        public int MenuOrder { get; set; } = 100;
        public bool IsPublished { get; set; } = true;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        _db.Pages.Add(new CmsPage
        {
            Title = Input.Title.Trim(),
            Slug = Input.Slug.Trim(),
            ContentHtml = Input.ContentHtml,
            ShowInMenu = Input.ShowInMenu,
            MenuOrder = Input.MenuOrder,
            IsPublished = Input.IsPublished,
            CreatedById = actor.Id,
        });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Страница добавлена.";
        return RedirectToPage("Index");
    }
}
