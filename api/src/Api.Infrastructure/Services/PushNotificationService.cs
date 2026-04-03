using System.Net;
using System.Text.Json;
using Api.Core.Aggregates.PushSubscriptionAggregate;
using Api.Core.Aggregates.PushSubscriptionAggregate.Specifications;
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

    public async Task SendToAllAsync(string title, string body, string? url = null, CancellationToken ct = default)
    {
        var subscriptions = await repo.ListAsync(new AllPushSubscriptionsSpec(), ct);
        if (subscriptions.Count == 0) return;

        var vapid = vapidOptions.Value;
        var client = new WebPushClient();
        client.SetVapidDetails(vapid.Subject, vapid.PublicKey, vapid.PrivateKey);

        var payload = JsonSerializer.Serialize(new { title, body, url }, JsonOpts);

        var expiredEndpoints = new List<string>();

        var tasks = subscriptions.Select(async sub =>
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

        // Dọn dẹp các subscription đã hết hạn
        if (expiredEndpoints.Count > 0)
        {
            var toDelete = subscriptions.Where(s => expiredEndpoints.Contains(s.Endpoint)).ToList();
            await repo.DeleteRangeAsync(toDelete, ct);
        }
    }
}
