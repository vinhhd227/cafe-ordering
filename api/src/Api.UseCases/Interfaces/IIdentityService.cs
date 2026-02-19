namespace Api.UseCases.Interfaces;

/// <summary>
/// Service interface for Identity/Authentication operations.
/// Abstracts UserManager and Identity Framework from the Use Cases layer.
/// </summary>
public interface IIdentityService
{
  /// <summary>
  /// Create a new application user. Returns the identity user ID (used as IdentityGuid in Customer).
  /// </summary>
  Task<Result<string>> CreateUserAsync(string email, string password);

  /// <summary>
  /// Authenticate user and generate JWT tokens.
  /// deviceInfo is optional — used to label the session (e.g., "Chrome/Windows", "iOS App").
  /// </summary>
  Task<Result<TokenResponse>> LoginAsync(string email, string password, string? deviceInfo = null);

  /// <summary>
  /// Refresh access token using refresh token.
  /// Rotates the token (old is revoked, new is issued — same device slot).
  /// </summary>
  Task<Result<TokenResponse>> RefreshTokenAsync(string accessToken, string refreshToken);

  /// <summary>
  /// Revoke a single refresh token (logout from one device).
  /// </summary>
  Task<Result> RevokeTokenAsync(string refreshToken, string reason = "Logout");

  /// <summary>
  /// Revoke all refresh tokens for a user (logout from all devices).
  /// Use after password change, account deactivation, or security breach.
  /// </summary>
  Task<Result> RevokeAllTokensAsync(string identityGuid, string reason = "Logout all devices");

  /// <summary>
  /// Change user password
  /// </summary>
  Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

  /// <summary>
  /// Update user email (synced from Customer aggregate via IdentityGuid).
  /// </summary>
  Task<Result> UpdateEmailAsync(string identityGuid, string newEmail);

  /// <summary>
  /// Deactivate user account (soft delete in Identity DB).
  /// </summary>
  Task<Result> DeactivateUserAsync(string identityGuid);

  /// <summary>
  /// Generate password reset token
  /// </summary>
  Task<Result> ResetPasswordAsync(string email);
}

/// <summary>
/// JWT token response
/// </summary>
public record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
