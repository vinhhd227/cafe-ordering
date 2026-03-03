namespace Api.UseCases.Sessions.AutoClose;

public record TryAutoCloseSessionCommand(Guid SessionId) : ICommand<Result>;
