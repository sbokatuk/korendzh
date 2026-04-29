using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Cms;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Admin.Cms.Services;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IMediaService _media;
    private readonly UserManager<AppUser> _users;

    public EditModel(AppDbContext db, IMediaService media, UserManager<AppUser> users)
    {
        _db = db;
        _media = media;
        _users = users;
    }

    [BindProperty] public ServiceFormInput Input { get; set; } = new();
    [BindProperty] public IFormFile? ImageFile { get; set; }
    [BindProperty] public bool ClearImage { get; set; }

    public bool NotFoundFlag { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var s = await _db.Services.FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) { NotFoundFlag = true; return Page(); }

        Input = new ServiceFormInput
        {
            Id = s.Id,
            Title = s.Title,
            Slug = s.Slug,
            ShortDescription = s.ShortDescription,
            DescriptionHtml = s.DescriptionHtml,
            PriceLabel = s.PriceLabel,
            DisplayOrder = s.DisplayOrder,
            ImageUrl = s.ImageUrl,
            IsPublished = s.IsPublished,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var s = await _db.Services.FirstOrDefaultAsync(x => x.Id == Input.Id);
        if (s is null) { NotFoundFlag = true; return Page(); }
        if (!ModelState.IsValid) return Page();

        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        if (ClearImage) Input.ImageUrl = null;
        if (ImageFile is not null && ImageFile.Length > 0)
        {
            try { Input.ImageUrl = (await _media.UploadAsync(ImageFile, actor.Id)).Url; }
            catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); return Page(); }
        }

        s.Title = Input.Title.Trim();
        s.Slug = Input.Slug.Trim();
        s.ShortDescription = Input.ShortDescription;
        s.DescriptionHtml = Input.DescriptionHtml;
        s.PriceLabel = Input.PriceLabel;
        s.DisplayOrder = Input.DisplayOrder;
        s.ImageUrl = Input.ImageUrl;
        s.IsPublished = Input.IsPublished;
        s.UpdatedAt = DateTime.UtcNow;
        s.UpdatedById = actor.Id;

        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Сохранено.";
        return RedirectToPage("Index");
    }
}
