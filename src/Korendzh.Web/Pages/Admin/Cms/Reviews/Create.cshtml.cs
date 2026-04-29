using System.ComponentModel.DataAnnotations;
using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Cms;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages.Admin.Cms.Reviews;

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

    [BindProperty] public InputModel Input { get; set; } = new()
    {
        ReviewDate = DateOnly.FromDateTime(DateTime.Today),
    };
    [BindProperty] public IFormFile? PhotoFile { get; set; }

    public class InputModel
    {
        [Required, MaxLength(150)] public string AuthorName { get; set; } = string.Empty;
        [Required, MaxLength(2000)] public string Text { get; set; } = string.Empty;
        [Range(0, 5)] public int Rating { get; set; } = 5;
        public DateOnly ReviewDate { get; set; }
        public int DisplayOrder { get; set; } = 100;
        public bool IsPublished { get; set; } = true;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        string? photoUrl = null;
        if (PhotoFile is not null && PhotoFile.Length > 0)
        {
            try { photoUrl = (await _media.UploadAsync(PhotoFile, actor.Id)).Url; }
            catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); return Page(); }
        }

        _db.Reviews.Add(new Review
        {
            AuthorName = Input.AuthorName.Trim(),
            Text = Input.Text.Trim(),
            Rating = Input.Rating,
            ReviewDate = Input.ReviewDate,
            DisplayOrder = Input.DisplayOrder,
            AuthorPhotoUrl = photoUrl,
            IsPublished = Input.IsPublished,
            CreatedById = actor.Id,
        });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Отзыв добавлен.";
        return RedirectToPage("Index");
    }
}
