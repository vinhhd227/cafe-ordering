namespace Api.UseCases.Auth.ActivateUser;

public record ActivateUserCommand(Guid UserId) : ICommand<Result>;
