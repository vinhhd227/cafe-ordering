using Api.Core.Aggregates.NotificationAggregate;

namespace Api.Core.Interfaces;

/// <summary>
/// Gửi thông báo đến người dùng theo role được cấu hình, đồng thời lưu lịch sử vào DB.
/// </summary>
public interface INotificationService
{
    Task SendAsync(
        NotificationType type,
        string title,
        string body,
        string? url = null,
        int? referenceId = null,
        CancellationToken ct = default);
}
