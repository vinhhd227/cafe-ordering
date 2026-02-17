namespace Api.UseCases.Interfaces;

/// <summary>
/// Service interface for Identity/Authentication operations.
/// Abstracts UserManager and Identity Framework from the Use Cases layer.
/// </summary>
public interface IIdentityService
{
  /// <summary>
  /// Create a new application user linked to a customer
  /// </summary>
  Task<Result> CreateUserAsync(string email, string password, string customerId);

  /// <summary>
  /// Authenticate user and generate JWT tokens
  /// </summary>
  Task<Result<TokenResponse>> LoginAsync(string email, string password);

  /// <summary>
  /// Refresh access token using refresh token
  /// </summary>
  Task<Result<TokenResponse>> RefreshTokenAsync(string accessToken, string refreshToken);

  /// <summary>
  /// Change user password
  /// </summary>
  Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

  /// <summary>
  /// Update user email (synced from Customer aggregate)
  /// </summary>
  Task<Result> UpdateEmailAsync(string customerId, string newEmail);

  /// <summary>
  /// Deactivate user account
  /// </summary>
  Task<Result> DeactivateUserAsync(string customerId);

  /// <summary>
  /// Generate password reset token
  /// </summary>
  Task<Result> ResetPasswordAsync(string email);
}

/// <summary>
/// JWT token response
/// </summary>
public record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
