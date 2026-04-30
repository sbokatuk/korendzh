using Korendzh.Domain;
using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Cms;
using Korendzh.Web.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Korendzh.Web.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly ICmsService _cms;
    private readonly AppOptions _app;

    public IndexModel(ICmsService cms, IOptions<AppOptions> app)
    {
        _cms = cms;
        _app = app.Value;
    }

    public SiteSettings Settings { get; private set; } = new();
    public List<Service> Services { get; private set; } = new();
    public List<Review> Reviews { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        // TrackingOnly: публичного лендинга нет — анонима отправляем на форму входа,
        // авторизованных распределяем по ролям.
        if (_app.IsTrackingOnly)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return LocalRedirect("/Account/Login");
            }
            if (User.IsInRole(Roles.Worker)
                && !User.IsInRole(Roles.Manager)
                && !User.IsInRole(Roles.Admin))
            {
                return LocalRedirect("/TimeEntries/Create");
            }
            return LocalRedirect("/Dashboard");
        }

        // Full mode: воркер сразу идёт к форме часов, остальные видят лендинг.
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
