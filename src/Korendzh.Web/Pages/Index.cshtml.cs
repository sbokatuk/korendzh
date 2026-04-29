using Korendzh.Domain;
using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Cms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly ICmsService _cms;

    public IndexModel(ICmsService cms)
    {
        _cms = cms;
    }

    public SiteSettings Settings { get; private set; } = new();
    public List<Service> Services { get; private set; } = new();
    public List<Review> Reviews { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        // Если зашёл авторизованный воркер (без управленческих ролей) — сразу к форме
        // ввода часов. Менеджеры и админы видят публичный лендинг как обычные посетители.
        if (User.Identity?.IsAuthenticated == true
            && User.IsInRole(Roles.Worker)
            && !User.IsInRole(Roles.Manager)
            && !User.IsInRole(Roles.Admin))
        {
            return LocalRedirect("/TimeEntries/Create");
        }

        Settings = await _cms.GetSiteSettingsAsync();
        Services = (await _cms.GetPublishedServicesAsync()).Take(6).ToList();
        Reviews = await _cms.GetPublishedReviewsAsync(take: 6);
        return Page();
    }
}
