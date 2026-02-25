using System.Security.Claims;

namespace Api.UseCases.Interfaces;

/// <summary>
/// Service interface for JWT token operations.
/// </summary>
public interface IJwtService
{
  /// <summary>
  /// Generate JWT access token (15 minutes) with user claims.
  /// Claims: sub, NameIdentifier, username, fullName, roles, permissions, staffId?, customerId?
  /// </summary>
  string GenerateAccessToken(
    Guid userId,
    string username,
    string fullName,
    IList<string> roles,
    IList<string> permissions,
    Guid? staffId = null,
    Guid? customerId = null);

  /// <summary>
  /// Generate a cryptographically-random refresh token string (64-byte Base64).
  /// </summary>
  string GenerateRefreshToken();

  /// <summary>
  /// Validate JWT token signature/issuer/audience (does NOT validate lifetime — used for refresh flow).
  /// Returns null if token is invalid.
  /// </summary>
  ClaimsPrincipal? ValidateToken(string token);
}
