namespace Api.UseCases.Auth.DeactivateUser;

/// <summary>
/// Command to deactivate a user account (prevents future logins) and revoke all their tokens.
/// Only Admins can execute this command.
/// </summary>
public record DeactivateUserCommand(Guid UserId) : ICommand<Result>;
