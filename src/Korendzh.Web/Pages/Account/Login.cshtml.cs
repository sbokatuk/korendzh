using System.ComponentModel.DataAnnotations;
using Korendzh.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<AppUser> _signIn;
    private readonly UserManager<AppUser> _users;
    private readonly IConfiguration _config;
    private readonly ILogger<LoginModel> _log;

    public LoginModel(SignInManager<AppUser> signIn, UserManager<AppUser> users, IConfiguration config, ILogger<LoginModel> log)
    {
        _signIn = signIn;
        _users = users;
        _config = config;
        _log = log;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public bool GoogleEnabled =>
        !string.IsNullOrEmpty(_config["Google:ClientId"]) &&
        !string.IsNullOrEmpty(_config["Google:ClientSecret"]);

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }

    public Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        return Task.CompletedTask;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        _log.LogInformation("Login attempt: Email='{Email}' (length={Len}), ModelValid={Valid}",
            Input.Email, Input.Email?.Length ?? 0, ModelState.IsValid);

        if (!ModelState.IsValid)
        {
            foreach (var (k, v) in ModelState)
            {
                foreach (var err in v.Errors)
                {
                    _log.LogInformation("Login ModelState error: {Key}: {Message}", k, err.ErrorMessage);
                }
            }
            return Page();
        }

        var user = await _users.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            _log.LogInformation("Login: user with email '{Email}' not found", Input.Email);
            ErrorMessage = "Неверный email или пароль.";
            return Page();
        }
        if (!user.IsActive)
        {
            _log.LogInformation("Login: user '{Email}' found but IsActive=false", Input.Email);
            ErrorMessage = "Неверный email или пароль.";
            return Page();
        }

        // Для диагностики проверим пароль отдельно — отделит «не тот пароль» от других сценариев.
        var passwordOk = await _users.CheckPasswordAsync(user, Input.Password);
        var lockedOut = await _users.IsLockedOutAsync(user);
        _log.LogInformation(
            "Login: user '{Email}' found. PasswordCheck={PwOk}, LockedOut={Locked}, AccessFailedCount={AFC}, EmailConfirmed={EC}, HasPassword={HP}, PasswordLen={PL}",
            Input.Email, passwordOk, lockedOut, user.AccessFailedCount, user.EmailConfirmed,
            await _users.HasPasswordAsync(user), Input.Password?.Length ?? 0);

        var result = await _signIn.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);
        _log.LogInformation("Login result for '{Email}': Succeeded={S}, IsLockedOut={L}, IsNotAllowed={NA}, RequiresTwoFactor={2FA}",
            Input.Email, result.Succeeded, result.IsLockedOut, result.IsNotAllowed, result.RequiresTwoFactor);

        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl ?? "/Dashboard");
        }
        if (result.IsLockedOut)
        {
            ErrorMessage = "Аккаунт временно заблокирован, попробуйте позже.";
        }
        else if (result.IsNotAllowed)
        {
            ErrorMessage = "Вход запрещён. Проверьте, подтверждён ли email или активна ли учётная запись.";
        }
        else
        {
            ErrorMessage = "Неверный email или пароль.";
        }
        return Page();
    }
}
