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

    public LoginModel(SignInManager<AppUser> signIn, UserManager<AppUser> users, IConfiguration config)
    {
        _signIn = signIn;
        _users = users;
        _config = config;
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
        if (!ModelState.IsValid) return Page();

        var user = await _users.FindByEmailAsync(Input.Email);
        if (user is null || !user.IsActive)
        {
            ErrorMessage = "Неверный email или пароль.";
            return Page();
        }

        var result = await _signIn.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl ?? "/");
        }
        if (result.IsLockedOut)
        {
            ErrorMessage = "Аккаунт временно заблокирован, попробуйте позже.";
        }
        else
        {
            ErrorMessage = "Неверный email или пароль.";
        }
        return Page();
    }
}
