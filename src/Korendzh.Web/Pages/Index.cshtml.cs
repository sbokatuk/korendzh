using Korendzh.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly UserManager<AppUser> _users;

    public IndexModel(UserManager<AppUser> users)
    {
        _users = users;
    }

    public string? FullName { get; set; }

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _users.GetUserAsync(User);
            FullName = user?.FullName;
        }
    }
}
