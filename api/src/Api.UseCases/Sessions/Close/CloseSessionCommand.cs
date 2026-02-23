namespace Api.UseCases.Sessions.Close;

public record CloseSessionCommand(Guid SessionId) : ICommand<Result>;
