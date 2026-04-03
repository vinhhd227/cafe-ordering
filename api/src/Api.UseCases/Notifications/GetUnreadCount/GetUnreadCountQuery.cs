namespace Api.UseCases.Notifications.GetUnreadCount;

public record GetUnreadCountQuery(string UserId) : IQuery<Result<int>>;
