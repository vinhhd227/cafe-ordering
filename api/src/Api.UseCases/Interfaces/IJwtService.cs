using System.Security.Claims;

namespace Api.UseCases.Interfaces;

/// <summary>
/// Service interface for JWT token operations.
/// </summary>
public interface IJwtService
{
  /// <summary>
  /// Generate JWT access token with user claims.
  /// </summary>
  /// <param name="userId">ApplicationUser ID (int, from Identity DB)</param>
  /// <param name="identityGuid">Same as userId.ToString() — used by business layer to lookup Customer</param>
  /// <param name="email">User email</param>
  /// <param name="roles">User roles</param>
  string GenerateAccessToken(int userId, string? identityGuid, string email, IList<string> roles);

  /// <summary>
  /// Generate refresh token
  /// </summary>
  string GenerateRefreshToken();

  /// <summary>
  /// Validate JWT token and return claims principal
  /// </summary>
  ClaimsPrincipal? ValidateToken(string token);
}
