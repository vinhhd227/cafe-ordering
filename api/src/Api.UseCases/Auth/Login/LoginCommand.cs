using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.Login;

/// <summary>
/// Command to authenticate a user with username + password.
/// Returns a JWT access token and a refresh token on success.
/// </summary>
public record LoginCommand(string Username, string Password) : ICommand<Result<AuthResponseDto>>;
