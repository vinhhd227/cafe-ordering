using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.RefreshToken;

/// <summary>
/// Command to rotate a refresh token and issue a new access token + refresh token pair.
/// </summary>
public record RefreshTokenCommand(string RefreshToken) : ICommand<Result<AuthResponseDto>>;
