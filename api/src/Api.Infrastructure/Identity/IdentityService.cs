using Api.UseCases.Interfaces;
using Ardalis.Result;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Implementation of IIdentityService using ASP.NET Core Identity.
/// Identity DB is separate from business DB — no cross-DB FK.
/// Supports multi-device login via UserRefreshTokens table.
/// </summary>
public class IdentityService : IIdentityService
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly IJwtService _jwtService;
  private readonly AppIdentityDbContext _identityDb;
  private readonly ILogger<IdentityService> _logger;

  // How long refresh tokens live
  private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

  // Max concurrent sessions per user (prevents token accumulation)
  private const int MaxActiveSessions = 5;

  public IdentityService(
    UserManager<ApplicationUser> userManager,
    IJwtService jwtService,
    AppIdentityDbContext identityDb,
    ILogger<IdentityService> logger)
  {
    _userManager = userManager;
    _jwtService = jwtService;
    _identityDb = identityDb;
    _logger = logger;
  }

  /// <summary>
  /// Creates a new user. Returns the identity user ID string (stored as Customer.IdentityGuid).
  /// </summary>
  public async Task<Result<string>> CreateUserAsync(string email, string password)
  {
    var user = new ApplicationUser
    {
      UserName = email,
      Email = email,
      EmailConfirmed = false,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    var result = await _userManager.CreateAsync(user, password);
    if (!result.Succeeded)
    {
      var errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
      return Result<string>.Error(errorMsg);
    }

    await _userManager.AddToRoleAsync(user, "Customer");

    _logger.LogInformation("Identity user created: {Email}", email);

    // Return identity user ID — caller stores this as Customer.IdentityGuid
    return Result<string>.Success(user.Id.ToString());
  }

  public async Task<Result<TokenResponse>> LoginAsync(string email, string password, string? deviceInfo = null)
  {
    var user = await _userManager.FindByEmailAsync(email);
    if (user is null || !user.IsActive)
      return Result<TokenResponse>.Unauthorized();

    if (!await _userManager.CheckPasswordAsync(user, password))
    {
      await _userManager.AccessFailedAsync(user);
      return Result<TokenResponse>.Unauthorized();
    }

    await _userManager.ResetAccessFailedCountAsync(user);

    var roles = await _userManager.GetRolesAsync(user);

    // identityGuid = userId.ToString() — business layer uses this to lookup Customer
    var identityGuid = user.Id.ToString();

    var accessToken = _jwtService.GenerateAccessToken(
      userId: user.Id,
      identityGuid: identityGuid,
      email: user.Email!,
      roles: roles);

    // Issue new refresh token (supports multi-device)
    var refreshToken = await IssueRefreshTokenAsync(user.Id, deviceInfo);

    user.UpdatedAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    return Result<TokenResponse>.Success(new TokenResponse(
      accessToken,
      refreshToken.Token,
      ExpiresIn: 3600));
  }

  public async Task<Result<TokenResponse>> RefreshTokenAsync(string accessToken, string refreshToken)
  {
    var principal = _jwtService.ValidateToken(accessToken);
    if (principal is null)
      return Result<TokenResponse>.Unauthorized();

    var userIdClaim = principal.FindFirst("userId")?.Value;
    if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
      return Result<TokenResponse>.Unauthorized();

    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user is null || !user.IsActive)
      return Result<TokenResponse>.Unauthorized();

    // Look up token in dedicated table (not on User entity)
    var storedToken = await _identityDb.UserRefreshTokens
      .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == refreshToken);

    if (storedToken is null || !storedToken.IsActive)
    {
      // Token not found or already revoked → possible token theft
      // Security: revoke ALL tokens for this user
      _logger.LogWarning(
        "Suspicious refresh attempt for user {UserId}: token not active. Revoking all tokens.",
        userId);
      await RevokeAllUserTokensAsync(userId, "Suspicious refresh attempt — possible token theft");
      return Result<TokenResponse>.Unauthorized();
    }

    // Token rotation: revoke old, issue new (same device info carries over)
    var deviceInfo = storedToken.DeviceInfo;
    storedToken.Revoke("Token rotation on refresh");
    await _identityDb.SaveChangesAsync();

    var roles = await _userManager.GetRolesAsync(user);
    var newAccessToken = _jwtService.GenerateAccessToken(
      userId: user.Id,
      identityGuid: user.Id.ToString(),
      email: user.Email!,
      roles: roles);

    var newRefreshToken = await IssueRefreshTokenAsync(user.Id, deviceInfo);

    user.UpdatedAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    return Result<TokenResponse>.Success(new TokenResponse(
      newAccessToken,
      newRefreshToken.Token,
      ExpiresIn: 3600));
  }

  public async Task<Result> RevokeTokenAsync(string refreshToken, string reason = "Logout")
  {
    var token = await _identityDb.UserRefreshTokens
      .FirstOrDefaultAsync(t => t.Token == refreshToken);

    if (token is null)
      return Result.NotFound("Refresh token not found");

    if (token.IsRevoked)
      return Result.Success(); // Already revoked — idempotent

    token.Revoke(reason);
    await _identityDb.SaveChangesAsync();

    _logger.LogInformation(
      "Refresh token revoked for user {UserId}. Reason: {Reason}",
      token.UserId, reason);

    return Result.Success();
  }

  public async Task<Result> RevokeAllTokensAsync(string identityGuid, string reason = "Logout all devices")
  {
    if (!int.TryParse(identityGuid, out var userId))
      return Result.NotFound("Invalid identity guid");

    await RevokeAllUserTokensAsync(userId, reason);
    return Result.Success();
  }

  public async Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
  {
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    if (!result.Succeeded)
      return Result.Error(string.Join("; ", result.Errors.Select(e => e.Description)));

    // Security best practice: revoke all sessions after password change
    await RevokeAllUserTokensAsync(userId, "Password changed");

    return Result.Success();
  }

  /// <summary>
  /// Update email in Identity DB. Call after Customer.UpdateEmail() in business DB.
  /// identityGuid = ApplicationUser.Id.ToString()
  /// </summary>
  public async Task<Result> UpdateEmailAsync(string identityGuid, string newEmail)
  {
    if (!int.TryParse(identityGuid, out var userId))
      return Result.NotFound("Invalid identity guid");

    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound("User not found");

    user.Email = newEmail;
    user.UserName = newEmail;
    user.EmailConfirmed = false;
    user.UpdatedAt = DateTime.UtcNow;

    var result = await _userManager.UpdateAsync(user);
    return result.Succeeded
      ? Result.Success()
      : Result.Error(string.Join("; ", result.Errors.Select(e => e.Description)));
  }

  /// <summary>
  /// Deactivate user in Identity DB (prevents login).
  /// identityGuid = ApplicationUser.Id.ToString()
  /// </summary>
  public async Task<Result> DeactivateUserAsync(string identityGuid)
  {
    if (!int.TryParse(identityGuid, out var userId))
      return Result.NotFound("Invalid identity guid");

    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    user.Deactivate();
    user.UpdatedAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    // Kick all devices when deactivating
    await RevokeAllUserTokensAsync(userId, "Account deactivated");

    return Result.Success();
  }

  public async Task<Result> ResetPasswordAsync(string email)
  {
    var user = await _userManager.FindByEmailAsync(email);
    if (user is null)
      return Result.Success(); // Don't reveal user existence

    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
    // TODO: Send email with token via IEmailSender
    _logger.LogInformation("Password reset token generated for {Email}", email);

    return Result.Success();
  }

  // ===== Private Helpers =====

  /// <summary>
  /// Issues a new refresh token for the given user/device.
  /// Enforces MaxActiveSessions by revoking the oldest if the limit is reached.
  /// </summary>
  private async Task<UserRefreshToken> IssueRefreshTokenAsync(int userId, string? deviceInfo)
  {
    // Housekeeping: remove physically expired tokens
    var expiredTokens = await _identityDb.UserRefreshTokens
      .Where(t => t.UserId == userId && t.ExpiresAt <= DateTime.UtcNow)
      .ToListAsync();
    _identityDb.UserRefreshTokens.RemoveRange(expiredTokens);

    // Enforce session cap — revoke oldest if already at limit
    var activeTokens = await _identityDb.UserRefreshTokens
      .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
      .OrderBy(t => t.CreatedAt)
      .ToListAsync();

    if (activeTokens.Count >= MaxActiveSessions)
    {
      var oldest = activeTokens.First();
      oldest.Revoke("Session limit reached — oldest session evicted");
      _logger.LogInformation(
        "User {UserId} hit max sessions ({Max}). Oldest session evicted.",
        userId, MaxActiveSessions);
    }

    // Issue the new token
    var token = new UserRefreshToken
    {
      UserId = userId,
      Token = _jwtService.GenerateRefreshToken(),
      DeviceInfo = deviceInfo,
      CreatedAt = DateTime.UtcNow,
      ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime)
    };

    _identityDb.UserRefreshTokens.Add(token);
    await _identityDb.SaveChangesAsync();

    return token;
  }

  /// <summary>
  /// Revokes all active (non-expired, non-revoked) refresh tokens for a user.
  /// </summary>
  private async Task RevokeAllUserTokensAsync(int userId, string reason)
  {
    var activeTokens = await _identityDb.UserRefreshTokens
      .Where(t => t.UserId == userId && t.RevokedAt == null)
      .ToListAsync();

    foreach (var token in activeTokens)
      token.Revoke(reason);

    await _identityDb.SaveChangesAsync();

    _logger.LogInformation(
      "Revoked {Count} refresh tokens for user {UserId}. Reason: {Reason}",
      activeTokens.Count, userId, reason);
  }
}
