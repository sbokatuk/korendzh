using System.Security.Claims;
using Korendzh.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages.Account;

/// <summary>
/// Обработчик внешних логинов (Google OAuth).
/// Поток:
///   1. POST с provider=Google + returnUrl → Challenge у Google middleware.
///   2. Google редиректит обратно сюда с ?handler=Callback.
///   3. Берём ExternalLoginInfo, ищем пользователя по email; если активен — логиним. Иначе — отказ.
///      Открытой регистрации через Google нет.
/// </summary>
[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<AppUser> _signIn;
    private readonly UserManager<AppUser> _users;
    private readonly ILogger<ExternalLoginModel> _log;

    public ExternalLoginModel(SignInManager<AppUser> signIn, UserManager<AppUser> users, ILogger<ExternalLoginModel> log)
    {
        _signIn = signIn;
        _users = users;
        _log = log;
    }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet() => RedirectToPage("/Account/Login");

    public IActionResult OnPost(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Page("/Account/ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signIn.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError is not null)
        {
            ErrorMessage = $"Провайдер вернул ошибку: {remoteError}";
            return Page();
        }

        var info = await _signIn.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "Не удалось получить данные от провайдера. Попробуйте снова.";
            return Page();
        }

        // Сначала пытаемся войти по сохранённой связке external login.
        var signIn = await _signIn.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (signIn.Succeeded)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        // Иначе — ищем активного пользователя по email и линкуем внешний логин.
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            ErrorMessage = "Провайдер не вернул email — войти через него нельзя.";
            return Page();
        }

        var user = await _users.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            ErrorMessage = "Аккаунт с таким email не найден. Попросите администратора отправить вам приглашение.";
            return Page();
        }

        var googleSub = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(googleSub))
        {
            user.GoogleSubject = googleSub;
            await _users.UpdateAsync(user);
        }

        var addLogin = await _users.AddLoginAsync(user, info);
        if (!addLogin.Succeeded)
        {
            ErrorMessage = "Не удалось привязать внешний логин: " + string.Join("; ", addLogin.Errors.Select(e => e.Description));
            _log.LogWarning("AddLogin failed for {UserId}: {Errors}", user.Id, ErrorMessage);
            return Page();
        }

        await _signIn.SignInAsync(user, isPersistent: false);
        return LocalRedirect(returnUrl ?? "/");
    }
}
