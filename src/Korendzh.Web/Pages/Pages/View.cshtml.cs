using Korendzh.Infrastructure.Cms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CmsPage = Korendzh.Domain.Cms.Page;

namespace Korendzh.Web.Pages.Pages;

[AllowAnonymous]
public class ViewModel : PageModel
{
    private readonly ICmsService _cms;
    public ViewModel(ICmsService cms) { _cms = cms; }

    /// <summary>Содержимое страницы. Имя Item, а не Page, чтобы не конфликтовать с PageModel.Page().</summary>
    public CmsPage? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        Item = await _cms.GetPageBySlugAsync(slug);
        return Page();
    }
}
