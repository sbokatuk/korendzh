using Korendzh.Infrastructure.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Korendzh.Web.Pages.Admin;

[Authorize(Policy = Auth.AuthorizationPolicies.AdminOnly)]
public class EmailTestModel : PageModel
{
    private readonly IEmailSender _email;
    private readonly ILogger<EmailTestModel> _log;

    public EmailTestModel(IEmailSender email, IOptions<EmailOptions> opt, ILogger<EmailTestModel> log)
    {
        _email = email;
        _log = log;
        Options = opt.Value;
    }

    [BindProperty] public string ToEmail { get; set; } = string.Empty;
    [BindProperty] public string Subject { get; set; } = "Тест email АрВи-транс";
    [BindProperty] public string Body { get; set; } =
        "<p>Это тестовое письмо из админки АрВи-транс.</p><p>Если оно дошло — SMTP настроен корректно.</p>";

    public EmailOptions Options { get; }
    public string? ResultMessage { get; private set; }
    public string? ResultDetail { get; private set; }
    public bool IsSuccess { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(ToEmail))
        {
            ResultMessage = "Укажите email получателя.";
            return Page();
        }

        try
        {
            _log.LogInformation("Email test: admin triggered test send to {To}", ToEmail);
            await _email.SendAsync(ToEmail, Subject, Body);
            IsSuccess = true;
            ResultMessage = $"Письмо отправлено на {ToEmail}. Проверьте входящие/спам.";
            _log.LogInformation("Email test: success, to={To}", ToEmail);
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            ResultMessage = ex.Message;
            ResultDetail = ex.ToString();
            _log.LogError(ex, "Email test: FAILED, to={To}", ToEmail);
        }

        return Page();
    }
}
