namespace Api.UseCases.Auth.Register;

/// <summary>
/// Command to register a new customer with authentication credentials.
/// Creates both Customer aggregate and ApplicationUser.
/// </summary>
public record RegisterCommand(
  string Username,
  string Email,
  string Password,
  string FullName) : ICommand<Result<RegisterResponse>>;

/// <summary>
/// Response after successful registration.
/// </summary>
public record RegisterResponse(string CustomerId, string Email);
