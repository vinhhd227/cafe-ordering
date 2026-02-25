namespace Api.UseCases.Interfaces;

/// <summary>
/// Service interface for Identity/Authentication operations.
/// Abstracts UserManager, SignInManager and Identity Framework from the Use Cases layer.
/// </summary>
public interface IIdentityService
{
  /// <summary>
  /// Create a new application user and assign the given role.
  /// Returns the new ApplicationUser.Id (Guid) as string for linking to domain aggregates.
  /// </summary>
  Task<Result<string>> CreateUserAsync(
    string username,
    string? email,
    string password,
    string fullName,
    string role);

  /// <summary>
  /// Authenticate user by username and generate JWT + refresh token.
  /// </summary>
  Task<Result<AuthResponseDto>> LoginAsync(string username, string password);

  /// <summary>
  /// Refresh access token using a refresh token.
  /// Rotates the token (old is revoked, new is issued).
  /// On suspicious usage (token not found or revoked), all user tokens are revoked.
  /// </summary>
  Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken);

  /// <summary>
  /// Create a staff account with an auto-generated temporary password.
  /// </summary>
  Task<Result<TemporaryPasswordDto>> CreateStaffAccountAsync(
    string username,
    string fullName,
    string role);

  /// <summary>
  /// Change user password. Revokes all refresh tokens after a successful change.
  /// </summary>
  Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

  /// <summary>
  /// Deactivate user account (prevents login) and revoke all refresh tokens.
  /// </summary>
  Task<Result> DeactivateUserAsync(Guid userId);

  /// <summary>
  /// Check whether a username is available (not yet taken).
  /// Used for real-time availability check during registration.
  /// </summary>
  Task<bool> IsUsernameAvailableAsync(string username);
}

/// <summary>Response returned after a successful login or token refresh.</summary>
public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);

/// <summary>Response returned after creating a staff account.</summary>
public record TemporaryPasswordDto(string Username, string TemporaryPassword);
