using Korendzh.Domain;
using Korendzh.Infrastructure.Auth;
using Korendzh.Infrastructure.Identity;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Web.Pages.Admin.Managers;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IPasswordResetService _resets;

    public IndexModel(AppDbContext db, UserManager<AppUser> users, IPasswordResetService resets)
    {
        _db = db;
        _users = users;
        _resets = resets;
    }

    public List<AppUser> Items { get; private set; } = new();
    public Dictionary<Guid, string> DivisionNames { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var managers = await _users.GetUsersInRoleAsync(Roles.Manager);
        Items = managers.OrderBy(u => u.FullName).ToList();
        DivisionNames = await _db.Divisions.ToDictionaryAsync(d => d.Id, d => d.Name);
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return RedirectToPage();
        u.IsActive = !u.IsActive;
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = u.IsActive ? "Активирован." : "Деактивирован.";
        return RedirectToPage();
    }

    /// <summary>
    /// Принудительный сброс пароля: отправляет менеджеру email со ссылкой для смены пароля.
    /// Сам пароль здесь не меняется — менеджер сам задаст новый, перейдя по ссылке.
    /// </summary>
    public async Task<IActionResult> OnPostResetPasswordAsync(Guid id)
    {
        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (target?.Email is null) return RedirectToPage();
        await _resets.RequestAsync(target.Email);
        TempData["StatusMessage"] = $"Письмо для сброса пароля отправлено на {target.Email}.";
        return RedirectToPage();
    }
}
