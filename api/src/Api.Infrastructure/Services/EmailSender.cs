using System.Net;
using SmtpClient = System.Net.Mail.SmtpClient;

namespace Api.Infrastructure.Services;

/// <summary>
///   Cấu hình SMTP
/// </summary>
public class SmtpSettings
{
  public string Host { get; set; } = string.Empty;
  public int Port { get; set; } = 587;
  public string From { get; set; } = string.Empty;
  public string? Username { get; set; }
  public string? Password { get; set; }
  public bool UseSsl { get; set; } = true;
}

/// <summary>
///   Gửi email qua SMTP
/// </summary>
public class EmailSender : IEmailSender
{
  private readonly ILogger<EmailSender> _logger;
  private readonly SmtpSettings _settings;

  public EmailSender(IOptions<SmtpSettings> settings, ILogger<EmailSender> logger)
  {
    _settings = settings.Value;
    _logger = logger;
  }

  public async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
  {
    await SendAsync(to, subject, body, false, ct);
  }

  public async Task SendHtmlEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
  {
    await SendAsync(to, subject, htmlBody, true, ct);
  }

  private async Task SendAsync(string to, string subject, string body, bool isHtml, CancellationToken ct)
  {
    try
    {
      using var client = new SmtpClient(_settings.Host, _settings.Port) { EnableSsl = _settings.UseSsl };

      if (!string.IsNullOrEmpty(_settings.Username))
      {
        client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
      }

      var message = new MailMessage(_settings.From, to, subject, body) { IsBodyHtml = isHtml };

      await client.SendMailAsync(message, ct);

      _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
      throw;
    }
  }
}
