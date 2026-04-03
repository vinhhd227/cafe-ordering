using Api.UseCases.Notifications.DTOs;

namespace Api.UseCases.NotificationConfigs.GetSettings;

public record GetNotificationSettingsQuery : IQuery<Result<NotificationSettingsDto>>;
