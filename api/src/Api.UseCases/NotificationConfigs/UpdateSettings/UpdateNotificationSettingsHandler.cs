using Api.Core.Aggregates.NotificationAggregate;
using Api.UseCases.Notifications.DTOs;

namespace Api.UseCases.NotificationConfigs.UpdateSettings;

public class UpdateNotificationSettingsHandler(IRepositoryBase<NotificationSettings> repo)
    : ICommandHandler<UpdateNotificationSettingsCommand, Result<NotificationSettingsDto>>
{
    public async ValueTask<Result<NotificationSettingsDto>> Handle(
        UpdateNotificationSettingsCommand cmd, CancellationToken ct)
    {
        if (cmd.RetentionDays < NotificationSettings.MinRetentionDays ||
            cmd.RetentionDays > NotificationSettings.MaxRetentionDays)
        {
            return Result.Invalid(new ValidationError(
                nameof(cmd.RetentionDays),
                $"RetentionDays must be between {NotificationSettings.MinRetentionDays} and {NotificationSettings.MaxRetentionDays}."));
        }

        var settings = (await repo.ListAsync(ct)).FirstOrDefault();
        if (settings is null) return Result.NotFound();

        settings.Update(cmd.RetentionDays);
        await repo.UpdateAsync(settings, ct);

        return Result.Success(new NotificationSettingsDto(settings.RetentionDays));
    }
}
