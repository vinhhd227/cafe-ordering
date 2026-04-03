using Api.UseCases.Notifications.DTOs;

namespace Api.UseCases.NotificationConfigs.Update;

public record UpdateNotificationConfigCommand(int Id, List<string> TargetRoles, bool IsEnabled)
    : ICommand<Result<NotificationConfigDto>>;
