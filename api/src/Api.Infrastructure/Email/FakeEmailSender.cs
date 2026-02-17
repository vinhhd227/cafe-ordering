namespace Api.Infrastructure.Email;

public class FakeEmailSender(ILogger<FakeEmailSender> logger) : IEmailSender
{
  private readonly ILogger<FakeEmailSender> _logger = logger;

  public Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
  {
    throw new NotImplementedException();
  }

  public Task SendHtmlEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
  {
    throw new NotImplementedException();
  }

  public Task SendEmailAsync(string to, string from, string subject, string body)
  {
    _logger.LogInformation("Not actually sending an email to {to} from {from} with subject {subject}", to, from,
      subject);
    return Task.CompletedTask;
  }
}
