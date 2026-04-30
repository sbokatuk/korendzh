using System.ComponentModel.DataAnnotations;
using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Cms;
using Korendzh.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages.Admin.Cms;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
public class SiteSettingsModel : PageModel
{
    private readonly ICmsService _cms;
    private readonly IMediaService _media;
    private readonly UserManager<AppUser> _users;

    public SiteSettingsModel(ICmsService cms, IMediaService media, UserManager<AppUser> users)
    {
        _cms = cms;
        _media = media;
        _users = users;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? HeroImageFile { get; set; }

    [BindProperty]
    public bool ClearHeroImage { get; set; }

    public class InputModel
    {
        [Required, MaxLength(100)] public string SiteName { get; set; } = "АрВи-транс";
        [MaxLength(200)] public string HeroTitle { get; set; } = string.Empty;
        [MaxLength(500)] public string HeroSubtitle { get; set; } = string.Empty;
        [MaxLength(500)] public string? HeroImageUrl { get; set; }
        [MaxLength(50)] public string Phone { get; set; } = string.Empty;
        [MaxLength(256)] public string Email { get; set; } = string.Empty;
        [MaxLength(300)] public string Address { get; set; } = string.Empty;
        [MaxLength(200)] public string WorkingHours { get; set; } = string.Empty;
        [MaxLength(500)] public string? InstagramUrl { get; set; }
        [MaxLength(500)] public string? TelegramUrl { get; set; }
        [MaxLength(500)] public string? VkUrl { get; set; }
    }

    public async Task OnGetAsync()
    {
        var s = await _cms.GetSiteSettingsAsync();
        Input = new InputModel
        {
            SiteName = s.SiteName,
            HeroTitle = s.HeroTitle,
            HeroSubtitle = s.HeroSubtitle,
            HeroImageUrl = s.HeroImageUrl,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            WorkingHours = s.WorkingHours,
            InstagramUrl = s.InstagramUrl,
            TelegramUrl = s.TelegramUrl,
            VkUrl = s.VkUrl,
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var actor = await _users.GetUserAsync(User);
        if (actor is null) return Forbid();

        if (ClearHeroImage) Input.HeroImageUrl = null;
        if (HeroImageFile is not null && HeroImageFile.Length > 0)
        {
            try
            {
                var asset = await _media.UploadAsync(HeroImageFile, actor.Id);
                Input.HeroImageUrl = asset.Url;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        await _cms.UpdateSiteSettingsAsync(new SiteSettings
        {
            SiteName = Input.SiteName,
            HeroTitle = Input.HeroTitle,
            HeroSubtitle = Input.HeroSubtitle,
            HeroImageUrl = Input.HeroImageUrl,
            Phone = Input.Phone,
            Email = Input.Email,
            Address = Input.Address,
            WorkingHours = Input.WorkingHours,
            InstagramUrl = Input.InstagramUrl,
            TelegramUrl = Input.TelegramUrl,
            VkUrl = Input.VkUrl,
        }, actor.Id);

        TempData["StatusMessage"] = "Настройки сохранены.";
        return RedirectToPage();
    }
}
