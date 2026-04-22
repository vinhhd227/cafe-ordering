using Api.Core.Aggregates.NotificationAggregate;
using Api.Core.Aggregates.NotificationAggregate.Specifications;
using Api.Core.Aggregates.PushSubscriptionAggregate;
using Api.Core.Aggregates.PushSubscriptionAggregate.Specifications;
using Api.Core.Interfaces;
using Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Services;

public class NotificationService(
    IRepositoryBase<Notification> notificationRepo,
    IReadRepositoryBase<NotificationConfig> configRepo,
    IReadRepositoryBase<PushSubscription> pushSubRepo,
    UserManager<ApplicationUser> userManager,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task SendAsync(
        NotificationType type,
        string title,
        string body,
        string? url = null,
        int? referenceId = null,
        string? pushBody = null,
        CancellationToken ct = default)
    {
        try
        {
            var config = await configRepo.FirstOrDefaultAsync(new NotificationConfigByTypeSpec(type.Name), ct);
            if (config is null || !config.IsEnabled || config.TargetRoles.Count == 0) return;

            // Lấy danh sách user theo roles được cấu hình
            var userIds = new List<string>();
            foreach (var role in config.TargetRoles)
            {
                var usersInRole = await userManager.GetUsersInRoleAsync(role);
                userIds.AddRange(usersInRole.Select(u => u.Id.ToString()));
            }

            userIds = userIds.Distinct().ToList();
            if (userIds.Count == 0) return;

            // Deduplication: nếu đã có notification cho referenceId + type này thì bỏ qua
            // Tránh trường hợp domain event fire nhiều lần trong cùng 1 request
            if (referenceId.HasValue)
            {
                var alreadyExists = await notificationRepo.AnyAsync(
                    new NotificationByReferenceSpec(type, referenceId.Value), ct);
                if (alreadyExists)
                {
                    logger.LogDebug("Duplicate notification skipped: type={Type} referenceId={ReferenceId}", type.Name, referenceId.Value);
                    return;
                }
            }

            // Lưu notification vào DB cho từng user
            // pushBody chứa thông tin chi tiết hơn (item list, tổng tiền) → ưu tiên lưu
            var storedBody = pushBody ?? body;
            var notifications = userIds
                .Select(uid => Notification.Create(uid, type, title, storedBody, url, referenceId))
                .ToList();
            await notificationRepo.AddRangeAsync(notifications, ct);

            // Lấy subscriptions ngay (DB query nhanh)
            var subs = await pushSubRepo.ListAsync(new PushSubscriptionsByUserIdsSpec(userIds), ct);
            if (subs.Count == 0) return;

            // Gửi push trong background — không block response
            var subSnapshots = subs
                .Select(s => new PushSubSnapshot(s.Endpoint, s.P256dh, s.Auth))
                .ToList();

            _ = Task.Run(async () =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
                await pushService.SendToSnapshotsAsync(subSnapshots, title, body, url, pushBody);
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification of type {Type}", type.Name);
        }
    }
}

