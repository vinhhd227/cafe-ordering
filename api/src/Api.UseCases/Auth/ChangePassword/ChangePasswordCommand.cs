namespace Api.UseCases.Auth.ChangePassword;

/// <summary>
/// Command to change the current authenticated user's password.
/// UserId is resolved from the JWT claims in the handler via ICurrentUserService.
/// </summary>
public record ChangePasswordCommand(
  string CurrentPassword,
  string NewPassword) : ICommand<Result>;
