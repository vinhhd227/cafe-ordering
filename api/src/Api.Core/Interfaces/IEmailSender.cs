namespace Api.Core.Interfaces;

/// <summary>
///   Service gửi email
/// </summary>
public interface IEmailSender
{
  /// <summary>
  ///   Gửi email đơn giản
  /// </summary>
  Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);

  /// <summary>
  ///   Gửi email với HTML body
  /// </summary>
  Task SendHtmlEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
