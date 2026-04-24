namespace Api.UseCases.Notifications.DeleteRead;

public record DeleteReadNotificationsCommand(string UserId) : ICommand<Result<int>>;
