using System.ComponentModel.DataAnnotations;
using Korendzh.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Korendzh.Web.Pages.Account;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private readonly IPasswordResetService _resets;

    public ForgotPasswordModel(IPasswordResetService resets)
    {
        _resets = resets;
    }

    [BindProperty, Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool Submitted { get; set; }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _resets.RequestAsync(Email);
        Submitted = true;
        return Page();
    }
}
