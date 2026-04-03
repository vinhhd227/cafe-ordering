namespace Api.UseCases.Push.Subscribe;

public record SubscribePushCommand(
    string UserId,
    string Endpoint,
    string P256dh,
    string Auth
) : ICommand<Result>;
