using System.ComponentModel.DataAnnotations;
using Korendzh.Infrastructure.Auth;
using Korendzh.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages.Account;

[AllowAnonymous]
public class AcceptInviteModel : PageModel
{
    private readonly IInviteService _invites;
    private readonly SignInManager<AppUser> _signIn;

    public AcceptInviteModel(IInviteService invites, SignInManager<AppUser> signIn)
    {
        _invites = invites;
        _signIn = signIn;
    }

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool Failed { get; set; }

    public class InputModel
    {
        [Required, MinLength(8), DataType(DataType.Password), Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password), ErrorMessage = "Пароли не совпадают"),
         DataType(DataType.Password), Display(Name = "Подтверждение пароля")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet()
    {
        Failed = string.IsNullOrEmpty(Token);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || string.IsNullOrEmpty(Token))
        {
            Failed = string.IsNullOrEmpty(Token);
            return Page();
        }

        try
        {
            var user = await _invites.AcceptInviteAsync(Token, Input.Password);
            if (user is null)
            {
                Failed = true;
                return Page();
            }
            await _signIn.SignInAsync(user, isPersistent: false);
            TempData["StatusMessage"] = "Пароль установлен. Добро пожаловать!";
            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
