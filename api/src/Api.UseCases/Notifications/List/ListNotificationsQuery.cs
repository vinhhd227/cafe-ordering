using Api.UseCases.Notifications.DTOs;

namespace Api.UseCases.Notifications.List;

public record ListNotificationsQuery(string UserId, int Page, int PageSize)
    : IQuery<Result<NotificationListDto>>;
