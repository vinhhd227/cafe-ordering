namespace Api.UseCases.Push.Unsubscribe;

public record UnsubscribePushCommand(string Endpoint) : ICommand<Result>;
