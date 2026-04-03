using Api.Core.Aggregates.NotificationAggregate;
using Api.Core.Aggregates.NotificationAggregate.Specifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Services;

public class NotificationCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupOldNotificationsAsync(stoppingToken);
        }
    }

    private async Task CleanupOldNotificationsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var notificationRepo = scope.ServiceProvider.GetRequiredService<IRepositoryBase<Notification>>();
            var settingsRepo = scope.ServiceProvider.GetRequiredService<IReadRepositoryBase<NotificationSettings>>();

            var settings = (await settingsRepo.ListAsync(ct)).FirstOrDefault();
            var retentionDays = settings?.RetentionDays ?? NotificationSettings.DefaultRetentionDays;

            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            var old = await notificationRepo.ListAsync(new OldNotificationsSpec(cutoff), ct);
            if (old.Count > 0)
            {
                await notificationRepo.DeleteRangeAsync(old, ct);
                logger.LogInformation("Cleaned up {Count} notifications older than {Days} days", old.Count, retentionDays);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clean up old notifications");
        }
    }
}
