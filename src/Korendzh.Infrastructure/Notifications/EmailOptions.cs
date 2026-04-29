namespace Korendzh.Infrastructure.Notifications;

public class EmailOptions
{
    public string Host { get; set; } = "smtp.example.com";
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@бокатюк.бел";
    public string FromName { get; set; } = "Korendzh";
    public string AppBaseUrl { get; set; } = "https://бокатюк.бел";
}
