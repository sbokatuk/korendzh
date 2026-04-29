using System.ComponentModel.DataAnnotations;
using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Cms;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Admin.Cms.Reviews;

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

    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public IFormFile? PhotoFile { get; set; }
    [BindProperty] public bool ClearPhoto { get; set; }

    public Review? Target { get; private set; }

    public class InputModel
    {
        public Guid Id { get; set; }
        [Required, MaxLength(150)] public string AuthorName { get; set; } = string.Empty;
        [Required, MaxLength(2000)] public string Text { get; set; } = string.Empty;
        [Range(0, 5)] public int Rating { get; set; } = 5;
        public DateOnly ReviewDate { get; set; }
        public int DisplayOrder { get; set; } = 100;
        public bool IsPublished { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Target = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id);
        if (Target is null) return Page();
        Input = new InputModel
        {
            Id = Target.Id,
            AuthorName = Target.AuthorName,
            Text = Target.Text,
            Rating = Target.Rating,
            ReviewDate = Target.ReviewDate,
            DisplayOrder = Target.DisplayOrder,
            IsPublished = Target.IsPublished,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Target = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == Input.Id);
        if (Target is null) return Page();
        if (!ModelState.IsValid) return Page();

        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        if (ClearPhoto) Target.AuthorPhotoUrl = null;
        if (PhotoFile is not null && PhotoFile.Length > 0)
        {
            try { Target.AuthorPhotoUrl = (await _media.UploadAsync(PhotoFile, actor.Id)).Url; }
            catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); return Page(); }
        }

        Target.AuthorName = Input.AuthorName.Trim();
        Target.Text = Input.Text.Trim();
        Target.Rating = Input.Rating;
        Target.ReviewDate = Input.ReviewDate;
        Target.DisplayOrder = Input.DisplayOrder;
        Target.IsPublished = Input.IsPublished;

        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Сохранено.";
        return RedirectToPage("Index");
    }
}
