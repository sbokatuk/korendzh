using Korendzh.Domain;
using Korendzh.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<AppUser> _users;

    public IndexModel(UserManager<AppUser> users)
    {
        _users = users;
    }

    public string? FullName { get; private set; }
    public string Role { get; private set; } = "Worker";
    public bool IsAdmin { get; private set; }
    public bool IsManagerOrAdmin { get; private set; }

    public async Task OnGetAsync()
    {
        var u = await _users.GetUserAsync(User);
        FullName = u?.FullName;
        IsAdmin = User.IsInRole(Roles.Admin);
        IsManagerOrAdmin = IsAdmin || User.IsInRole(Roles.Manager);
        Role = IsAdmin ? "администратор" : User.IsInRole(Roles.Manager) ? "менеджер" : "воркер";
    }
}
