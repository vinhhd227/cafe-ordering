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
    Guid? customerId = null,
    string? avatarUrl = null);

  /// <summary>
  /// Generate a cryptographically-random refresh token string (64-byte Base64).
  /// </summary>
  string GenerateRefreshToken();

  /// <summary>
  /// Extracts claims from a token without validating its lifetime.
  /// <para>
  /// ⚠️ ONLY use for the refresh-token flow — this method intentionally bypasses expiry validation.
  /// Do NOT use for authenticating active requests (ASP.NET middleware handles that with lifetime checks).
  /// </para>
  /// Returns null if the token signature, issuer, or audience is invalid.
  /// </summary>
  ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
