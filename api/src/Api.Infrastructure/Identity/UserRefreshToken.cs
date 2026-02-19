namespace Api.Infrastructure.Identity;

/// <summary>
/// Represents a refresh token issued to a specific device/session.
/// Stored in Identity schema — one row per active session.
/// Supports multi-device login: one user can have multiple active tokens.
/// </summary>
public class UserRefreshToken
{
  public int Id { get; set; }

  /// <summary>FK to AspNetUsers (identity.Users)</summary>
  public int UserId { get; set; }

  /// <summary>The refresh token value (random 64-byte Base64 string)</summary>
  public string Token { get; set; } = string.Empty;

  /// <summary>
  /// Optional: Identifies the device/client (e.g. "Chrome/Windows", "iOS App").
  /// Useful for "Logout from this device" feature.
  /// </summary>
  public string? DeviceInfo { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime ExpiresAt { get; set; }

  /// <summary>
  /// Null means token is still valid.
  /// Non-null means token was revoked (logout, password change, etc.)
  /// </summary>
  public DateTime? RevokedAt { get; set; }

  /// <summary>Why this token was revoked (optional, for audit)</summary>
  public string? RevokedReason { get; set; }

  // === Navigation ===
  public ApplicationUser User { get; set; } = null!;

  // === Computed ===
  public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
  public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
  public bool IsRevoked => RevokedAt is not null;

  // === Methods ===

  public void Revoke(string reason = "Manual revocation")
  {
    RevokedAt = DateTime.UtcNow;
    RevokedReason = reason;
  }
}
