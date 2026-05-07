using System.Security.Cryptography;
using System.Text;
using Api.UseCases.Auth.Login;
using Api.UseCases.Interfaces;
using Ardalis.Result;
using Microsoft.AspNetCore.Identity;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Implementation of IIdentityService using ASP.NET Core Identity.
/// Identity DB is separate from business DB — no cross-DB FK.
/// </summary>
public class IdentityService(
  UserManager<ApplicationUser> userManager,
  SignInManager<ApplicationUser> signInManager,
  RoleManager<ApplicationRole> roleManager,
  IJwtService jwtService,
  AppIdentityDbContext identityDb,
  ILogger<IdentityService> logger)
  : IIdentityService
{
  private static readonly TimeSpan ShortSession = TimeSpan.FromDays(1);
  private static readonly TimeSpan LongSession  = TimeSpan.FromDays(30);

  /// <summary>
  /// Creates a new application user and assigns a role.
  /// Returns the identity user ID (Guid.ToString()) for linking to domain aggregates.
  /// </summary>
  public async Task<Result<string>> CreateUserAsync(
    string username,
    string? email,
    string password,
    string fullName,
    string role,
    CancellationToken ct = default)
  {
    await using var tx = await identityDb.Database.BeginTransactionAsync(ct);

    var user = new ApplicationUser
    {
      UserName = username,
      Email = email,
      FullName = fullName,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    var result = await userManager.CreateAsync(user, password);
    if (!result.Succeeded)
    {
      var errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
      return Result<string>.Error(errorMsg);
    }

    var roleResult = await userManager.AddToRoleAsync(user, role);
    if (!roleResult.Succeeded)
    {
      return Result<string>.Error(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
    }

    await tx.CommitAsync(ct);
    logger.LogInformation("Identity user created: {Username} with role {Role}", username, role);

    return Result<string>.Success(user.Id.ToString());
  }

  public async Task<Result<AuthResponseDto>> LoginAsync(string username, string password, AppType app, bool rememberMe, CancellationToken ct = default)
  {
    username = username.Trim();
    var user = await userManager.FindByNameAsync(username);
    if (user is null)
      return Result<AuthResponseDto>.Unauthorized();

    if (!user.IsActive)
      return Result<AuthResponseDto>.Forbidden("account_inactive");

    if (await userManager.IsLockedOutAsync(user))
      return Result<AuthResponseDto>.Forbidden("account_locked");

    var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
    if (!signInResult.Succeeded)
      return Result<AuthResponseDto>.Unauthorized();

    var roles = await userManager.GetRolesAsync(user);
    var permissions = await GetUserPermissionsAsync(roles, ct);

    // App-level access check
    var requiredPermission = app == AppType.Admin ? "admin.access" : "customer.access";
    if (!permissions.Contains(requiredPermission))
      return Result<AuthResponseDto>.Forbidden("access_denied");

    var accessToken = jwtService.GenerateAccessToken(
      userId: user.Id,
      username: user.UserName!,
      fullName: user.FullName,
      roles: roles,
      permissions: permissions,
      staffId: user.StaffId,
      customerId: user.CustomerId,
      avatarUrl: user.AvatarUrl);

    var rawRefreshToken = await IssueRefreshTokenAsync(user.Id, rememberMe, ct);

    var expiresAt = DateTime.UtcNow.Add(rememberMe ? LongSession : ShortSession);

    return Result<AuthResponseDto>.Success(new AuthResponseDto(accessToken, rawRefreshToken, expiresAt, rememberMe));
  }

  public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
  {
    var tokenHash = HashToken(refreshToken);
    var storedToken = await identityDb.RefreshTokens
      .FirstOrDefaultAsync(t => t.Token == tokenHash, ct);

    if (storedToken is null || storedToken.IsRevoked)
    {
      // Token not found or already revoked; possible token theft
      if (storedToken is not null)
      {
        logger.LogWarning(
          "Suspicious refresh attempt for user {UserId}: token already revoked. Revoking all tokens.",
          storedToken.UserId);
        await RevokeAllUserTokensAsync(storedToken.UserId, ct);
      }
      return Result<AuthResponseDto>.Unauthorized();
    }

    if (storedToken.ExpiresAt <= DateTime.UtcNow)
      return Result<AuthResponseDto>.Unauthorized();

    var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());
    if (user is null || !user.IsActive)
      return Result<AuthResponseDto>.Unauthorized();

    // Token rotation: revoke old, issue new
    storedToken.Revoke();
    await identityDb.SaveChangesAsync(ct);

    var roles = await userManager.GetRolesAsync(user);
    var permissions = await GetUserPermissionsAsync(roles, ct);

    var newAccessToken = jwtService.GenerateAccessToken(
      userId: user.Id,
      username: user.UserName!,
      fullName: user.FullName,
      roles: roles,
      permissions: permissions,
      staffId: user.StaffId,
      customerId: user.CustomerId,
      avatarUrl: user.AvatarUrl);

    var rememberMe = storedToken.RememberMe;
    var rawRefreshToken = await IssueRefreshTokenAsync(user.Id, rememberMe, ct);

    var expiresAt = DateTime.UtcNow.Add(rememberMe ? LongSession : ShortSession);

    return Result<AuthResponseDto>.Success(new AuthResponseDto(newAccessToken, rawRefreshToken, expiresAt, rememberMe));
  }

  public async Task<Result<TemporaryPasswordDto>> CreateStaffAccountAsync(
    string username,
    string fullName,
    string role,
    CancellationToken ct = default)
  {
    var existing = await userManager.FindByNameAsync(username);
    if (existing is not null)
      return Result<TemporaryPasswordDto>.Conflict($"Username '{username}' is already taken.");

    var tempPassword = GenerateTemporaryPassword();

    await using var tx = await identityDb.Database.BeginTransactionAsync(ct);

    var user = new ApplicationUser
    {
      UserName = username,
      FullName = fullName,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    var result = await userManager.CreateAsync(user, tempPassword);
    if (!result.Succeeded)
    {
      var errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
      return Result<TemporaryPasswordDto>.Error(errorMsg);
    }

    var roleResult = await userManager.AddToRoleAsync(user, role);
    if (!roleResult.Succeeded)
    {
      return Result<TemporaryPasswordDto>.Error(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
    }

    await tx.CommitAsync(ct);
    logger.LogInformation("Staff account created: {Username} with role {Role}", username, role);

    return Result<TemporaryPasswordDto>.Success(new TemporaryPasswordDto(username, tempPassword));
  }

  public async Task<Result<TemporaryPasswordDto>> ResetUserPasswordAsync(Guid userId, CancellationToken ct = default)
  {
    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result<TemporaryPasswordDto>.NotFound();

    var tempPassword = GenerateTemporaryPassword();

    var token = await userManager.GeneratePasswordResetTokenAsync(user);
    var resetResult = await userManager.ResetPasswordAsync(user, token, tempPassword);
    if (!resetResult.Succeeded)
      return Result<TemporaryPasswordDto>.Error(string.Join("; ", resetResult.Errors.Select(e => e.Description)));

    await RevokeAllUserTokensAsync(userId, ct);

    logger.LogInformation("Password reset for user {UserId}", userId);

    return Result<TemporaryPasswordDto>.Success(new TemporaryPasswordDto(user.UserName!, tempPassword));
  }

  public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
  {
    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    if (!result.Succeeded)
    {
      var errors = result.Errors
        .Select(e => new ValidationError(e.Code, e.Description))
        .ToList();
      return Result.Invalid(errors);
    }

    // Security best practice: revoke all sessions after password change
    await RevokeAllUserTokensAsync(userId, ct);

    logger.LogInformation("Password changed for user {UserId}", userId);

    return Result.Success();
  }

  public async Task<bool> IsUsernameAvailableAsync(string username, CancellationToken ct = default)
  {
    var user = await userManager.FindByNameAsync(username);
    return user is null;
  }

  public async Task<Result> DeactivateUserAsync(Guid userId, CancellationToken ct = default)
  {
    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    user.Deactivate();
    user.UpdatedAt = DateTime.UtcNow;
    await userManager.UpdateAsync(user);

    // Kick all devices when deactivating
    await RevokeAllUserTokensAsync(userId, ct);

    logger.LogInformation("User {UserId} deactivated", userId);

    return Result.Success();
  }

  public async Task<Result<PagedUsersDto>> GetUsersAsync(
    int page,
    int pageSize,
    string? search,
    string? role,
    bool? isActive,
    CancellationToken ct = default)
  {
    var query = userManager.Users
      .AsNoTracking()
      .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
      .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
      query = query.Where(u =>
        u.UserName!.Contains(search) ||
        u.FullName.Contains(search));

    if (!string.IsNullOrWhiteSpace(role))
      query = query.Where(u =>
        u.UserRoles.Any(ur => ur.Role.Name == role));

    if (isActive.HasValue)
      query = query.Where(u => u.IsActive == isActive.Value);

    var total = await query.CountAsync(ct);

    var users = await query
      .OrderByDescending(u => u.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync(ct);

    var items = users.Select(u => new UserDto(
      u.Id,
      u.UserName!,
      u.FullName,
      u.Email,
      u.UserRoles.Select(ur => ur.Role.Name!).ToList(),
      u.IsActive,
      u.CreatedAt
    )).ToList();

    return Result<PagedUsersDto>.Success(new PagedUsersDto(items, total, page, pageSize));
  }

  public async Task<Result> UpdateUserAsync(Guid userId, string fullName, string? email, CancellationToken ct = default)
  {
    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    user.FullName = fullName;
    user.Email = email;
    user.UpdatedAt = DateTime.UtcNow;

    var result = await userManager.UpdateAsync(user);
    if (!result.Succeeded)
      return Result.Error(string.Join("; ", result.Errors.Select(e => e.Description)));

    logger.LogInformation("User {UserId} profile updated", userId);
    return Result.Success();
  }

  public async Task<Result<string>> UpdateAvatarAsync(Guid userId, string avatarUrl, CancellationToken ct = default)
  {
    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result<string>.NotFound();

    user.AvatarUrl = avatarUrl;
    user.UpdatedAt = DateTime.UtcNow;

    var result = await userManager.UpdateAsync(user);
    if (!result.Succeeded)
      return Result<string>.Error(string.Join("; ", result.Errors.Select(e => e.Description)));

    logger.LogInformation("Avatar updated for user {UserId}", userId);
    return Result<string>.Success(avatarUrl);
  }

  public async Task<Result> ActivateUserAsync(Guid userId, CancellationToken ct = default)
  {
    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    user.Activate();
    user.UpdatedAt = DateTime.UtcNow;
    await userManager.UpdateAsync(user);

    logger.LogInformation("User {UserId} activated", userId);
    return Result.Success();
  }

  public async Task<Result> ChangeUserRoleAsync(Guid userId, string newRole, CancellationToken ct = default)
  {
    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    await using var tx = await identityDb.Database.BeginTransactionAsync(ct);

    var currentRoles = await userManager.GetRolesAsync(user);
    var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
    if (!removeResult.Succeeded)
    {
      return Result.Error(string.Join("; ", removeResult.Errors.Select(e => e.Description)));
    }

    var addResult = await userManager.AddToRoleAsync(user, newRole);
    if (!addResult.Succeeded)
    {
      return Result.Error(string.Join("; ", addResult.Errors.Select(e => e.Description)));
    }

    await tx.CommitAsync(ct);
    logger.LogInformation("User {UserId} role changed to {Role}", userId, newRole);
    return Result.Success();
  }

  public async Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
  {
    var user = await userManager.Users
      .AsNoTracking()
      .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
      .FirstOrDefaultAsync(u => u.Id == userId, ct);

    if (user is null)
      return Result<UserDto>.NotFound();

    return Result<UserDto>.Success(new UserDto(
      user.Id,
      user.UserName!,
      user.FullName,
      user.Email,
      user.UserRoles.Select(ur => ur.Role.Name!).ToList(),
      user.IsActive,
      user.CreatedAt));
  }

  // ===== Role Management =====

  public async Task<Result<PagedRolesDto>> GetRolesAsync(int page, int pageSize, string? search, CancellationToken ct = default)
  {
    var query = roleManager.Roles
      .AsNoTracking()
      .Include(r => r.UserRoles)
      .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
      query = query.Where(r =>
        r.Name!.Contains(search) ||
        (r.Description != null && r.Description.Contains(search)));

    var total = await query.CountAsync(ct);

    var roles = await query
      .OrderBy(r => r.Name)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync(ct);

    var items = roles.Select(r => new RoleDto(
      r.Id,
      r.Name!,
      r.Description,
      r.IsActive,
      r.UserRoles.Count,
      r.CreatedAt
    )).ToList();

    return Result<PagedRolesDto>.Success(new PagedRolesDto(items, total, page, pageSize));
  }

  public async Task<Result<RoleDto>> GetRoleByIdAsync(Guid roleId, CancellationToken ct = default)
  {
    var role = await roleManager.Roles
      .AsNoTracking()
      .Include(r => r.UserRoles)
      .FirstOrDefaultAsync(r => r.Id == roleId, ct);

    if (role is null)
      return Result<RoleDto>.NotFound();

    return Result<RoleDto>.Success(new RoleDto(
      role.Id, role.Name!, role.Description, role.IsActive, role.UserRoles.Count, role.CreatedAt));
  }

  public async Task<Result> CreateRoleAsync(string name, string? description, CancellationToken ct = default)
  {
    var existing = await roleManager.FindByNameAsync(name);
    if (existing is not null)
      return Result.Conflict($"Role '{name}' already exists.");

    var role = ApplicationRole.Create(name, description);
    var result = await roleManager.CreateAsync(role);
    if (!result.Succeeded)
      return Result.Error(string.Join("; ", result.Errors.Select(e => e.Description)));

    logger.LogInformation("Role created: {RoleName}", name);
    return Result.Success();
  }

  public async Task<Result> UpdateRoleAsync(Guid roleId, string name, string? description, CancellationToken ct = default)
  {
    var role = await roleManager.FindByIdAsync(roleId.ToString());
    if (role is null)
      return Result.NotFound();

    if (!string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase))
    {
      var existing = await roleManager.FindByNameAsync(name);
      if (existing is not null)
        return Result.Conflict($"Role '{name}' already exists.");
    }

    role.Name = name;
    role.NormalizedName = name.ToUpperInvariant();
    role.UpdateDescription(description);

    var result = await roleManager.UpdateAsync(role);
    if (!result.Succeeded)
      return Result.Error(string.Join("; ", result.Errors.Select(e => e.Description)));

    logger.LogInformation("Role updated: {RoleId} → {RoleName}", roleId, name);
    return Result.Success();
  }

  public async Task<Result> DeleteRoleAsync(Guid roleId, CancellationToken ct = default)
  {
    var role = await roleManager.Roles
      .Include(r => r.UserRoles)
      .FirstOrDefaultAsync(r => r.Id == roleId, ct);

    if (role is null)
      return Result.NotFound();

    if (role.UserRoles.Count > 0)
      return Result.Conflict(
        $"Cannot delete role '{role.Name}': {role.UserRoles.Count} user(s) are still assigned to it.");

    var result = await roleManager.DeleteAsync(role);
    if (!result.Succeeded)
      return Result.Error(string.Join("; ", result.Errors.Select(e => e.Description)));

    logger.LogInformation("Role deleted: {RoleName}", role.Name);
    return Result.Success();
  }

  // ===== Role Permissions =====

  public async Task<Result<List<RolePermissionDto>>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default)
  {
    var role = await roleManager.FindByIdAsync(roleId.ToString());
    if (role is null)
      return Result<List<RolePermissionDto>>.NotFound();

    var assigned = await identityDb.RoleClaims
      .Where(rc => rc.RoleId == roleId && rc.ClaimType == "permission")
      .Select(rc => rc.ClaimValue!)
      .ToHashSetAsync(ct);

    var result = PermissionRegistry.All
      .Select(kv => new RolePermissionDto(kv.Key, kv.Value, assigned.Contains(kv.Key)))
      .OrderBy(p => p.Value)
      .ToList();

    return Result<List<RolePermissionDto>>.Success(result);
  }

  public async Task<Result> SetRolePermissionsAsync(Guid roleId, IList<string> permissions, CancellationToken ct = default)
  {
    var role = await roleManager.FindByIdAsync(roleId.ToString());
    if (role is null)
      return Result.NotFound();

    var unknown = permissions.Where(p => !PermissionRegistry.All.ContainsKey(p)).ToList();
    if (unknown.Count > 0)
      return Result.Invalid(new ValidationError("permissions",
        $"Unknown permissions: {string.Join(", ", unknown)}"));

    await using var tx = await identityDb.Database.BeginTransactionAsync(ct);

    // Remove all existing permission claims
    var existing = await identityDb.RoleClaims
      .Where(rc => rc.RoleId == roleId && rc.ClaimType == "permission")
      .ToListAsync(ct);
    identityDb.RoleClaims.RemoveRange(existing);

    // Add new permission claims with description from registry
    foreach (var perm in permissions.Distinct())
    {
      identityDb.RoleClaims.Add(new ApplicationRoleClaim
      {
        RoleId      = roleId,
        ClaimType   = "permission",
        ClaimValue  = perm,
        Description = PermissionRegistry.GetDescription(perm)
      });
    }

    await identityDb.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);

    logger.LogInformation("Permissions updated for role {RoleId}: [{Permissions}]",
      roleId, string.Join(", ", permissions));

    return Result.Success();
  }

  // ===== Private Helpers =====

  private async Task<string> IssueRefreshTokenAsync(Guid userId, bool rememberMe, CancellationToken ct)
  {
    var expiredTokens = await identityDb.RefreshTokens
      .Where(t => t.UserId == userId && t.ExpiresAt <= DateTime.UtcNow)
      .ToListAsync(ct);
    identityDb.RefreshTokens.RemoveRange(expiredTokens);

    var rawToken = jwtService.GenerateRefreshToken();
    identityDb.RefreshTokens.Add(new RefreshToken
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Token = HashToken(rawToken),
      CreatedAt = DateTime.UtcNow,
      ExpiresAt = DateTime.UtcNow.Add(rememberMe ? LongSession : ShortSession),
      IsRevoked = false,
      RememberMe = rememberMe
    });

    await identityDb.SaveChangesAsync(ct);
    return rawToken;
  }

  public async Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
  {
    var storedToken = await identityDb.RefreshTokens
      .FirstOrDefaultAsync(t => t.Token == HashToken(refreshToken), ct);

    if (storedToken is null)
      return Result.NotFound();

    storedToken.Revoke();
    await identityDb.SaveChangesAsync(ct);

    return Result.Success();
  }

  private async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct)
  {
    var activeTokens = await identityDb.RefreshTokens
      .Where(t => t.UserId == userId && !t.IsRevoked)
      .ToListAsync(ct);

    foreach (var token in activeTokens)
      token.Revoke();

    await identityDb.SaveChangesAsync(ct);

    logger.LogInformation("Revoked {Count} refresh tokens for user {UserId}", activeTokens.Count, userId);
  }

  private async Task<IList<string>> GetUserPermissionsAsync(IList<string> roles, CancellationToken ct)
  {
    if (roles.Count == 0) return [];

    var normalizedNames = roles.Select(r => r.ToUpperInvariant()).ToList();

    var roleIds = await identityDb.Roles
      .Where(r => normalizedNames.Contains(r.NormalizedName!))
      .Select(r => r.Id)
      .ToListAsync(ct);

    return await identityDb.RoleClaims
      .Where(rc => rc.ClaimType == "permission" && roleIds.Contains(rc.RoleId))
      .Select(rc => rc.ClaimValue!)
      .Distinct()
      .ToListAsync(ct);
  }

  private static string HashToken(string token)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    return Convert.ToHexString(bytes);
  }

  private static string GenerateTemporaryPassword(int length = 8)
  {
    const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    const string lower = "abcdefghijkmnopqrstuvwxyz";
    const string digits = "23456789";
    const string all = upper + lower + digits;

    if (length < 3)
      throw new ArgumentException("Password length must be at least 3.");

    var chars = new char[length];

    chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
    chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
    chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];

    for (int i = 3; i < length; i++)
      chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

    // Fisher-Yates shuffle
    for (int i = chars.Length - 1; i > 0; i--)
    {
      int j = RandomNumberGenerator.GetInt32(i + 1);
      (chars[i], chars[j]) = (chars[j], chars[i]);
    }

    return new string(chars);
  }
}
