using Api.Core.Aggregates.PushSubscriptionAggregate;

namespace Api.Core.Interfaces;

/// <summary>Plain data snapshot of a push subscription — safe to use across DI scopes.</summary>
public sealed record PushSubSnapshot(string Endpoint, string P256dh, string Auth);

/// <summary>
/// Gửi web push notification đến một tập subscriptions cụ thể.
/// </summary>
public interface IPushNotificationService
{
    Task SendToSubscriptionsAsync(
        IEnumerable<PushSubscription> subscriptions,
        string title,
        string body,
        string? url = null,
        CancellationToken ct = default);

    Task SendToSnapshotsAsync(
        IEnumerable<PushSubSnapshot> snapshots,
        string title,
        string body,
        string? url = null,
        string? detail = null,
        CancellationToken ct = default);
}
