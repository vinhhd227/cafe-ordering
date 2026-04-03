using Api.Core.Aggregates.NotificationAggregate;
using Api.UseCases.Notifications.DTOs;

namespace Api.UseCases.NotificationConfigs.GetSettings;

public class GetNotificationSettingsHandler(IReadRepositoryBase<NotificationSettings> repo)
    : IQueryHandler<GetNotificationSettingsQuery, Result<NotificationSettingsDto>>
{
    public async ValueTask<Result<NotificationSettingsDto>> Handle(
        GetNotificationSettingsQuery q, CancellationToken ct)
    {
        // Single-row table — lấy record đầu tiên (luôn tồn tại sau seed)
        var settings = (await repo.ListAsync(ct)).FirstOrDefault();
        if (settings is null) return Result.NotFound();

        return Result.Success(new NotificationSettingsDto(settings.RetentionDays));
    }
}
