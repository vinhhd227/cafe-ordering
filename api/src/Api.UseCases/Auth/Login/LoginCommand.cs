using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.Login;

/// <summary>
/// Command to authenticate a user with username + password for a specific app.
/// Returns a JWT access token and a refresh token on success.
/// Fails with Forbidden if the user's role does not have access to the requested app.
/// </summary>
public record LoginCommand(string Username, string Password, AppType App, bool RememberMe) : ICommand<Result<AuthResponseDto>>;
