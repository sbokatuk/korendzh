using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Cms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages.Services;

[AllowAnonymous]
public class DetailModel : PageModel
{
    private readonly ICmsService _cms;
    public DetailModel(ICmsService cms) { _cms = cms; }

    public Service? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        Item = await _cms.GetServiceBySlugAsync(slug);
        return Page();
    }
}
