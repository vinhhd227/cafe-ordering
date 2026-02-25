namespace Api.UseCases.Auth.UpdateUser;

public record UpdateUserCommand(
  Guid UserId,
  string FullName,
  string? Email) : ICommand<Result>;
