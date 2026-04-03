namespace Api.UseCases.Notifications.MarkRead;

public record MarkNotificationReadCommand(int Id, string UserId) : ICommand<Result>;
