using System.Net;
using System.Text.Json;
using Api.Core.Aggregates.PushSubscriptionAggregate;
using Api.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;
using DomainPushSub = Api.Core.Aggregates.PushSubscriptionAggregate.PushSubscription;
using BrowserPushSub = WebPush.PushSubscription;

namespace Api.Infrastructure.Services;

public class PushNotificationService(
    IRepositoryBase<DomainPushSub> repo,
    IOptions<VapidSettings> vapidOptions,
    ILogger<PushNotificationService> logger) : IPushNotificationService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task SendToSubscriptionsAsync(
        IEnumerable<DomainPushSub> subscriptions,
        string title,
        string body,
        string? url = null,
        CancellationToken ct = default)
    {
        var subList = subscriptions.ToList();
        if (subList.Count == 0) return;

        var snapshots = subList.Select(s => new PushSubSnapshot(s.Endpoint, s.P256dh, s.Auth));
        var expired = await SendCoreAsync(snapshots, title, body, url);

        if (expired.Count > 0)
        {
            var toDelete = subList.Where(s => expired.Contains(s.Endpoint)).ToList();
            await repo.DeleteRangeAsync(toDelete, ct);
        }
    }

    public async Task SendToSnapshotsAsync(
        IEnumerable<PushSubSnapshot> snapshots,
        string title,
        string body,
        string? url = null,
        CancellationToken ct = default)
    {
        var expired = await SendCoreAsync(snapshots, title, body, url);

        if (expired.Count > 0)
        {
            // Load full entities để xóa — chỉ cần endpoint để match
            var allSubs = await repo.ListAsync(ct);
            var toDelete = allSubs.Where(s => expired.Contains(s.Endpoint)).ToList();
            if (toDelete.Count > 0)
                await repo.DeleteRangeAsync(toDelete, ct);
        }
    }

    private async Task<List<string>> SendCoreAsync(
        IEnumerable<PushSubSnapshot> snapshots,
        string title,
        string body,
        string? url)
    {
        var list = snapshots.ToList();
        if (list.Count == 0) return [];

        var vapid = vapidOptions.Value;
        var client = new WebPushClient();
        client.SetVapidDetails(vapid.Subject, vapid.PublicKey, vapid.PrivateKey);

        var payload = JsonSerializer.Serialize(new { title, body, url }, JsonOpts);
        var expiredEndpoints = new List<string>();

        var tasks = list.Select(async sub =>
        {
            try
            {
                var pushSub = new BrowserPushSub(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSub, payload);
            }
            catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone)
            {
                logger.LogInformation("Push subscription expired, will remove: {Endpoint}", sub.Endpoint);
                lock (expiredEndpoints) expiredEndpoints.Add(sub.Endpoint);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send push notification to {Endpoint}", sub.Endpoint);
            }
        });

        await Task.WhenAll(tasks);
        return expiredEndpoints;
    }
}
