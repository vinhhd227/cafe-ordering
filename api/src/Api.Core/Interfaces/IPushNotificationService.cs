namespace Api.Core.Interfaces;

/// <summary>
/// Gửi web push notification đến tất cả subscriptions đang active.
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Gửi notification đến tất cả subscriptions.
    /// </summary>
    Task SendToAllAsync(string title, string body, string? url = null, CancellationToken ct = default);
}
