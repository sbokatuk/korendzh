using System.ComponentModel.DataAnnotations;
using Korendzh.Domain;
using Korendzh.Infrastructure.Auth;
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

        // Браузер для <input type="email"> с IDN-доменом отдаёт Punycode-форму.
        // Чтобы найти пользователя независимо от того, в какой форме email хранится в БД,
        // ищем сначала по введённому значению, потом по нормализованному ASCII (Punycode),
        // потом по Unicode-форме (на случай если в БД хранится исходник).
        // Input.Email и Input.Password гарантированно не null после валидации ModelState ([Required]).
        var inputEmail = Input.Email ?? string.Empty;
        var inputPassword = Input.Password ?? string.Empty;
        var asciiEmail = EmailNormalizer.ToAscii(inputEmail);
        var user = await _users.FindByEmailAsync(inputEmail)
                   ?? await _users.FindByEmailAsync(asciiEmail);
        if (user is null)
        {
            _log.LogInformation("Login: user not found. Tried '{InputEmail}' and '{Ascii}'", inputEmail, asciiEmail);
            ErrorMessage = "Неверный email или пароль.";
            return Page();
        }
        if (!user.IsActive)
        {
            _log.LogInformation("Login: user '{Email}' found but IsActive=false", inputEmail);
            ErrorMessage = "Неверный email или пароль.";
            return Page();
        }

        // Для диагностики проверим пароль отдельно — отделит «не тот пароль» от других сценариев.
        var passwordOk = await _users.CheckPasswordAsync(user, inputPassword);
        var lockedOut = await _users.IsLockedOutAsync(user);
        _log.LogInformation(
            "Login: user '{Email}' found. PasswordCheck={PwOk}, LockedOut={Locked}, AccessFailedCount={AFC}, EmailConfirmed={EC}, HasPassword={HP}, PasswordLen={PL}",
            inputEmail, passwordOk, lockedOut, user.AccessFailedCount, user.EmailConfirmed,
            await _users.HasPasswordAsync(user), inputPassword.Length);

        var result = await _signIn.PasswordSignInAsync(user, inputPassword, Input.RememberMe, lockoutOnFailure: true);
        _log.LogInformation("Login result for '{Email}': Succeeded={S}, IsLockedOut={L}, IsNotAllowed={NA}, RequiresTwoFactor={2FA}",
            inputEmail, result.Succeeded, result.IsLockedOut, result.IsNotAllowed, result.RequiresTwoFactor);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            // Воркер сразу попадает на форму ввода часов; менеджер/админ — в дашборд.
            var roles = await _users.GetRolesAsync(user);
            var isPrivileged = roles.Contains(Roles.Admin) || roles.Contains(Roles.Manager);
            return LocalRedirect(isPrivileged ? "/Dashboard" : "/TimeEntries/Create");
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
