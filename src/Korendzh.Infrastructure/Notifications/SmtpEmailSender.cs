using System.Diagnostics;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Korendzh.Infrastructure.Notifications;

/// <summary>
/// MailKit-based SMTP sender. Креды берутся из EmailOptions (Plesk Application Settings).
/// Подробные логи на каждом шаге — для диагностики проблем с доставкой через stdout-лог.
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
        // Sanity-проверка конфига до попытки коннекта.
        if (string.IsNullOrWhiteSpace(_opt.Host) ||
            string.Equals(_opt.Host, "smtp.example.com", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogError(
                "SMTP NOT CONFIGURED: Email:Host='{Host}'. Письмо для {To} (subject='{Subject}') не будет отправлено. " +
                "Заполните Email:* в \\httpdocs\\appsettings.Local.json (или env-переменные) и перезапустите приложение.",
                _opt.Host, toEmail, subject);
            throw new InvalidOperationException("Email:Host is not configured.");
        }
        if (string.IsNullOrWhiteSpace(_opt.FromAddress))
        {
            _log.LogError("SMTP NOT CONFIGURED: Email:FromAddress пуст. Письмо для {To} не будет отправлено.", toEmail);
            throw new InvalidOperationException("Email:FromAddress is not configured.");
        }

        _log.LogInformation(
            "SMTP send: to={To}, subject='{Subject}', host={Host}:{Port}, useStartTls={Tls}, fromAddress={From}, hasAuth={HasAuth}",
            toEmail, subject, _opt.Host, _opt.Port, _opt.UseStartTls, _opt.FromAddress,
            !string.IsNullOrEmpty(_opt.User));

        var msg = new MimeMessage();
        try
        {
            msg.From.Add(new MailboxAddress(_opt.FromName ?? string.Empty, _opt.FromAddress));
            msg.To.Add(MailboxAddress.Parse(toEmail));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "SMTP failed to build message: from={From}, to={To}", _opt.FromAddress, toEmail);
            throw;
        }

        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var smtp = new SmtpClient();
        var secure = _opt.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
        var sw = Stopwatch.StartNew();

        try
        {
            _log.LogInformation("SMTP connecting to {Host}:{Port} ({Secure})...", _opt.Host, _opt.Port, secure);
            await smtp.ConnectAsync(_opt.Host, _opt.Port, secure, ct);
            _log.LogInformation("SMTP connected to {Host}:{Port} in {Ms} ms (capabilities: {Caps})",
                _opt.Host, _opt.Port, sw.ElapsedMilliseconds, smtp.Capabilities);

            if (!string.IsNullOrEmpty(_opt.User))
            {
                _log.LogInformation("SMTP authenticating as {User}...", _opt.User);
                await smtp.AuthenticateAsync(_opt.User, _opt.Password, ct);
                _log.LogInformation("SMTP authenticated as {User}", _opt.User);
            }
            else
            {
                _log.LogWarning(
                    "SMTP no Email:User configured — пытаемся отправить анонимно. Большинство публичных SMTP это запретят.");
            }

            var serverResponse = await smtp.SendAsync(msg, ct);
            _log.LogInformation(
                "SMTP send OK: to={To}, subject='{Subject}', total={Ms} ms, response='{Response}'",
                toEmail, subject, sw.ElapsedMilliseconds, serverResponse);
        }
        catch (AuthenticationException ex)
        {
            _log.LogError(ex,
                "SMTP AUTH FAILED: host={Host}, user={User}. Проверьте логин/пароль в Email:User / Email:Password.",
                _opt.Host, _opt.User);
            throw;
        }
        catch (SmtpCommandException ex)
        {
            _log.LogError(ex,
                "SMTP COMMAND FAILED: code={Code}, status={Status}, host={Host}, to={To}. Часто это значит, что 'From' " +
                "не разрешён на этом SMTP, или адрес получателя отвергнут.",
                ex.StatusCode, ex.ErrorCode, _opt.Host, toEmail);
            throw;
        }
        catch (SmtpProtocolException ex)
        {
            _log.LogError(ex, "SMTP PROTOCOL ERROR: host={Host}", _opt.Host);
            throw;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "SMTP UNEXPECTED FAILURE: host={Host}, to={To}", _opt.Host, toEmail);
            throw;
        }
        finally
        {
            try { await smtp.DisconnectAsync(true, ct); }
            catch (Exception ex) { _log.LogWarning(ex, "SMTP disconnect failed"); }
        }
    }
}
