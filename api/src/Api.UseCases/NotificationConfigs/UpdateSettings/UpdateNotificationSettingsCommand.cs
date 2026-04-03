using Api.UseCases.Notifications.DTOs;

namespace Api.UseCases.NotificationConfigs.UpdateSettings;

public record UpdateNotificationSettingsCommand(int RetentionDays) : ICommand<Result<NotificationSettingsDto>>;
