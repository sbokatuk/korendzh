using System.ComponentModel.DataAnnotations;
using Korendzh.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages.Account;

[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly IPasswordResetService _resets;

    public ResetPasswordModel(IPasswordResetService resets)
    {
        _resets = resets;
    }

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool Failed { get; set; }
    public bool Done { get; set; }

    public class InputModel
    {
        [Required, MinLength(8), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password)), DataType(DataType.Password)]
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

        var ok = await _resets.ConfirmAsync(Token, Input.Password);
        if (!ok)
        {
            Failed = true;
            return Page();
        }
        Done = true;
        return Page();
    }
}
