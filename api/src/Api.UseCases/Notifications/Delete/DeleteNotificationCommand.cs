namespace Api.UseCases.Notifications.Delete;

public record DeleteNotificationCommand(int Id, string UserId) : ICommand<Result>;
