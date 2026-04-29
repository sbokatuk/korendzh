using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Cms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages.Reviews;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly ICmsService _cms;
    public IndexModel(ICmsService cms) { _cms = cms; }

    public List<Review> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await _cms.GetPublishedReviewsAsync();
    }
}
