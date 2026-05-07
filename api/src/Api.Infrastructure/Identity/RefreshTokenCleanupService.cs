using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Identity;

public class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan RevokedRetention = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

            var now = DateTime.UtcNow;
            var revokedCutoff = now - RevokedRetention;

            var deleted = await db.RefreshTokens
                .Where(t => t.ExpiresAt <= now || (t.IsRevoked && t.CreatedAt <= revokedCutoff))
                .ExecuteDeleteAsync(ct);

            if (deleted > 0)
                logger.LogInformation("Refresh token cleanup: deleted {Count} rows", deleted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Refresh token cleanup failed");
        }
    }
}
