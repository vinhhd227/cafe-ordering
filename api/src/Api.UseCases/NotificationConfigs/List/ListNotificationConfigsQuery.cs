using Api.UseCases.Notifications.DTOs;

namespace Api.UseCases.NotificationConfigs.List;

public record ListNotificationConfigsQuery : IQuery<Result<IEnumerable<NotificationConfigDto>>>;
