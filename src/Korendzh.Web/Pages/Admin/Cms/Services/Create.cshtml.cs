using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Cms;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages.Admin.Cms.Services;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IMediaService _media;
    private readonly UserManager<AppUser> _users;

    public CreateModel(AppDbContext db, IMediaService media, UserManager<AppUser> users)
    {
        _db = db;
        _media = media;
        _users = users;
    }

    [BindProperty] public ServiceFormInput Input { get; set; } = new();
    [BindProperty] public IFormFile? ImageFile { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        if (ImageFile is not null && ImageFile.Length > 0)
        {
            try { Input.ImageUrl = (await _media.UploadAsync(ImageFile, actor.Id)).Url; }
            catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); return Page(); }
        }

        _db.Services.Add(new Service
        {
            Title = Input.Title.Trim(),
            Slug = Input.Slug.Trim(),
            ShortDescription = Input.ShortDescription,
            DescriptionHtml = Input.DescriptionHtml,
            PriceLabel = Input.PriceLabel,
            DisplayOrder = Input.DisplayOrder,
            ImageUrl = Input.ImageUrl,
            IsPublished = Input.IsPublished,
            CreatedById = actor.Id,
        });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Услуга добавлена.";
        return RedirectToPage("Index");
    }
}
