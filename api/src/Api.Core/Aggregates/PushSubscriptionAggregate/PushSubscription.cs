namespace Api.Core.Aggregates.PushSubscriptionAggregate;

/// <summary>
/// Lưu trữ browser push subscription của một user.
/// Mỗi browser/thiết bị có một subscription riêng (unique bởi Endpoint).
/// </summary>
public class PushSubscription : AuditableEntity<int>, IAggregateRoot
{
    public string UserId { get; private set; } = default!;

    /// <summary>Push endpoint URL cấp bởi browser vendor (Google FCM / Apple APNs)</summary>
    public string Endpoint { get; private set; } = default!;

    /// <summary>ECDH public key của client (base64url)</summary>
    public string P256dh { get; private set; } = default!;

    /// <summary>Auth secret (base64url)</summary>
    public string Auth { get; private set; } = default!;

    private PushSubscription() { }

    public static PushSubscription Create(string userId, string endpoint, string p256dh, string auth)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.NullOrWhiteSpace(endpoint);
        Guard.Against.NullOrWhiteSpace(p256dh);
        Guard.Against.NullOrWhiteSpace(auth);

        return new PushSubscription
        {
            UserId = userId,
            Endpoint = endpoint,
            P256dh = p256dh,
            Auth = auth,
        };
    }

    public void Update(string p256dh, string auth)
    {
        P256dh = p256dh;
        Auth = auth;
    }
}
