namespace Api.UseCases.Notifications.MarkAllRead;

public record MarkAllNotificationsReadCommand(string UserId) : ICommand<Result>;
