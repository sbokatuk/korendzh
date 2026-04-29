using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Korendzh.Infrastructure.Notifications;

/// <summary>
/// MailKit-based SMTP sender. Креды берутся из EmailOptions (Plesk Application Settings).
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opt;
    private readonly ILogger<SmtpEmailSender> _log;

    public SmtpEmailSender(IOptions<EmailOptions> opt, ILogger<SmtpEmailSender> log)
    {
        _opt = opt.Value;
        _log = log;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_opt.FromName, _opt.FromAddress));
        msg.To.Add(MailboxAddress.Parse(toEmail));
        msg.Subject = subject;

        var body = new BodyBuilder { HtmlBody = htmlBody };
        msg.Body = body.ToMessageBody();

        using var smtp = new SmtpClient();
        var secure = _opt.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;

        try
        {
            await smtp.ConnectAsync(_opt.Host, _opt.Port, secure, ct);
            if (!string.IsNullOrEmpty(_opt.User))
            {
                await smtp.AuthenticateAsync(_opt.User, _opt.Password, ct);
            }
            await smtp.SendAsync(msg, ct);
        }
        finally
        {
            try { await smtp.DisconnectAsync(true, ct); }
            catch (Exception ex) { _log.LogWarning(ex, "SMTP disconnect failed"); }
        }
    }
}
