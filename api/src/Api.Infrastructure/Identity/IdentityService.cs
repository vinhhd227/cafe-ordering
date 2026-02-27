using System.Security.Cryptography;
using System.Text;
using Api.UseCases.Interfaces;
using Ardalis.Result;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Implementation of IIdentityService using ASP.NET Core Identity.
/// Identity DB is separate from business DB — no cross-DB FK.
/// </summary>
public class IdentityService : IIdentityService
{
  private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

  private readonly UserManager<ApplicationUser> _userManager;
  private readonly SignInManager<ApplicationUser> _signInManager;
  private readonly RoleManager<ApplicationRole> _roleManager;
  private readonly IJwtService _jwtService;
  private readonly AppIdentityDbContext _identityDb;
  private readonly ILogger<IdentityService> _logger;

  public IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<ApplicationRole> roleManager,
    IJwtService jwtService,
    AppIdentityDbContext identityDb,
    ILogger<IdentityService> logger)
  {
    _userManager = userManager;
    _signInManager = signInManager;
    _roleManager = roleManager;
    _jwtService = jwtService;
    _identityDb = identityDb;
    _logger = logger;
  }

  /// <summary>
  /// Creates a new application user and assigns a role.
  /// Returns the identity user ID (Guid.ToString()) for linking to domain aggregates.
  /// </summary>
  public async Task<Result<string>> CreateUserAsync(
    string username,
    string? email,
    string password,
    string fullName,
    string role)
  {
    var user = new ApplicationUser
    {
      UserName = username,
      Email = email,
      FullName = fullName,
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

    await _userManager.AddToRoleAsync(user, role);

    _logger.LogInformation("Identity user created: {Username} with role {Role}", username, role);

    return Result<string>.Success(user.Id.ToString());
  }

  public async Task<Result<AuthResponseDto>> LoginAsync(string username, string password)
  {
    var user = await _userManager.FindByNameAsync(username);
    if (user is null || !user.IsActive)
      return Result<AuthResponseDto>.Unauthorized();

    // Check lockout before attempting sign-in
    if (await _userManager.IsLockedOutAsync(user))
      return Result<AuthResponseDto>.Unauthorized();

    var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
    if (!signInResult.Succeeded)
      return Result<AuthResponseDto>.Unauthorized();

    var roles = await _userManager.GetRolesAsync(user);
    var permissions = await GetUserPermissionsAsync(roles);

    var accessToken = _jwtService.GenerateAccessToken(
      userId: user.Id,
      username: user.UserName!,
      fullName: user.FullName,
      roles: roles,
      permissions: permissions,
      staffId: user.StaffId,
      customerId: user.CustomerId);

    var refreshToken = await IssueRefreshTokenAsync(user.Id);

    user.UpdatedAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    var expiresAt = DateTime.UtcNow.AddDays(7);

    return Result<AuthResponseDto>.Success(new AuthResponseDto(accessToken, refreshToken.Token, expiresAt));
  }

  public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken)
  {
    var storedToken = await _identityDb.RefreshTokens
      .FirstOrDefaultAsync(t => t.Token == refreshToken);

    if (storedToken is null || storedToken.IsRevoked)
    {
      // Token not found or already revoked → possible token theft
      if (storedToken is not null)
      {
        _logger.LogWarning(
          "Suspicious refresh attempt for user {UserId}: token already revoked. Revoking all tokens.",
          storedToken.UserId);
        await RevokeAllUserTokensAsync(storedToken.UserId);
      }
      return Result<AuthResponseDto>.Unauthorized();
    }

    if (storedToken.ExpiresAt <= DateTime.UtcNow)
      return Result<AuthResponseDto>.Unauthorized();

    var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
    if (user is null || !user.IsActive)
      return Result<AuthResponseDto>.Unauthorized();

    // Token rotation: revoke old, issue new
    storedToken.Revoke();
    await _identityDb.SaveChangesAsync();

    var roles = await _userManager.GetRolesAsync(user);
    var permissions = await GetUserPermissionsAsync(roles);

    var newAccessToken = _jwtService.GenerateAccessToken(
      userId: user.Id,
      username: user.UserName!,
      fullName: user.FullName,
      roles: roles,
      permissions: permissions,
      staffId: user.StaffId,
      customerId: user.CustomerId);

    var newRefreshToken = await IssueRefreshTokenAsync(user.Id);

    user.UpdatedAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    var expiresAt = DateTime.UtcNow.AddDays(7);

    return Result<AuthResponseDto>.Success(new AuthResponseDto(newAccessToken, newRefreshToken.Token, expiresAt));
  }

  public async Task<Result<TemporaryPasswordDto>> CreateStaffAccountAsync(
    string username,
    string fullName,
    string role)
  {
    var existing = await _userManager.FindByNameAsync(username);
    if (existing is not null)
      return Result<TemporaryPasswordDto>.Conflict($"Username '{username}' is already taken.");

    var tempPassword = GenerateTemporaryPassword();

    var user = new ApplicationUser
    {
      UserName = username,
      FullName = fullName,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    var result = await _userManager.CreateAsync(user, tempPassword);
    if (!result.Succeeded)
    {
      var errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
      return Result<TemporaryPasswordDto>.Error(errorMsg);
    }

    await _userManager.AddToRoleAsync(user, role);

    _logger.LogInformation("Staff account created: {Username} with role {Role}", username, role);

    return Result<TemporaryPasswordDto>.Success(new TemporaryPasswordDto(username, tempPassword));
  }

  public async Task<Result<TemporaryPasswordDto>> ResetUserPasswordAsync(Guid userId)
  {
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result<TemporaryPasswordDto>.NotFound();

    var tempPassword = GenerateTemporaryPassword();

    var removeResult = await _userManager.RemovePasswordAsync(user);
    if (!removeResult.Succeeded)
      return Result<TemporaryPasswordDto>.Error(string.Join("; ", removeResult.Errors.Select(e => e.Description)));

    var addResult = await _userManager.AddPasswordAsync(user, tempPassword);
    if (!addResult.Succeeded)
      return Result<TemporaryPasswordDto>.Error(string.Join("; ", addResult.Errors.Select(e => e.Description)));

    await RevokeAllUserTokensAsync(userId);

    _logger.LogInformation("Password reset for user {UserId}", userId);

    return Result<TemporaryPasswordDto>.Success(new TemporaryPasswordDto(user.UserName!, tempPassword));
  }

  public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
  {
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    if (!result.Succeeded)
      return Result.Error(string.Join("; ", result.Errors.Select(e => e.Description)));

    // Security best practice: revoke all sessions after password change
    await RevokeAllUserTokensAsync(userId);

    _logger.LogInformation("Password changed for user {UserId}", userId);

    return Result.Success();
  }

  public async Task<bool> IsUsernameAvailableAsync(string username)
  {
    var user = await _userManager.FindByNameAsync(username);
    return user is null;
  }

  public async Task<Result> DeactivateUserAsync(Guid userId)
  {
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    user.Deactivate();
    user.UpdatedAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    // Kick all devices when deactivating
    await RevokeAllUserTokensAsync(userId);

    _logger.LogInformation("User {UserId} deactivated", userId);

    return Result.Success();
  }

  public async Task<Result<PagedUsersDto>> GetUsersAsync(
    int page,
    int pageSize,
    string? search,
    string? role,
    bool? isActive)
  {
    var query = _userManager.Users
      .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
      .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
      query = query.Where(u =>
        u.UserName!.Contains(search) ||
        u.FullName.Contains(search));

    if (!string.IsNullOrWhiteSpace(role))
      query = query.Where(u =>
        u.UserRoles.Any(ur => ur.Role!.Name == role));

    if (isActive.HasValue)
      query = query.Where(u => u.IsActive == isActive.Value);

    var total = await query.CountAsync();

    var users = await query
      .OrderByDescending(u => u.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync();

    var items = users.Select(u => new UserDto(
      u.Id,
      u.UserName!,
      u.FullName,
      u.Email,
      u.UserRoles.Select(ur => ur.Role!.Name!).ToList(),
      u.IsActive,
      u.CreatedAt
    )).ToList();

    return Result<PagedUsersDto>.Success(new PagedUsersDto(items, total, page, pageSize));
  }

  public async Task<Result> UpdateUserAsync(Guid userId, string fullName, string? email)
  {
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    user.FullName = fullName;
    user.Email = email;
    user.UpdatedAt = DateTime.UtcNow;

    var result = await _userManager.UpdateAsync(user);
    if (!result.Succeeded)
      return Result.Error(string.Join("; ", result.Errors.Select(e => e.Description)));

    _logger.LogInformation("User {UserId} profile updated", userId);
    return Result.Success();
  }

  public async Task<Result> ActivateUserAsync(Guid userId)
  {
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    user.Activate();
    user.UpdatedAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    _logger.LogInformation("User {UserId} activated", userId);
    return Result.Success();
  }

  public async Task<Result> ChangeUserRoleAsync(Guid userId, string newRole)
  {
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    var currentRoles = await _userManager.GetRolesAsync(user);
    await _userManager.RemoveFromRolesAsync(user, currentRoles);

    var addResult = await _userManager.AddToRoleAsync(user, newRole);
    if (!addResult.Succeeded)
      return Result.Error(string.Join("; ", addResult.Errors.Select(e => e.Description)));

    _logger.LogInformation("User {UserId} role changed to {Role}", userId, newRole);
    return Result.Success();
  }

  public async Task<Result<UserDto>> GetUserByIdAsync(Guid userId)
  {
    var user = await _userManager.Users
      .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
      .FirstOrDefaultAsync(u => u.Id == userId);

    if (user is null)
      return Result<UserDto>.NotFound();

    return Result<UserDto>.Success(new UserDto(
      user.Id,
      user.UserName!,
      user.FullName,
      user.Email,
      user.UserRoles.Select(ur => ur.Role!.Name!).ToList(),
      user.IsActive,
      user.CreatedAt));
  }

  // ===== Role Management =====

  public async Task<Result<PagedRolesDto>> GetRolesAsync(int page, int pageSize, string? search)
  {
    var query = _roleManager.Roles
      .Include(r => r.UserRoles)
      .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
      query = query.Where(r =>
        r.Name!.Contains(search) ||
        (r.Description != null && r.Description.Contains(search)));

    var total = await query.CountAsync();

    var roles = await query
      .OrderBy(r => r.Name)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync();

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

  public async Task<Result<RoleDto>> GetRoleByIdAsync(Guid roleId)
  {
    var role = await _roleManager.Roles
      .Include(r => r.UserRoles)
      .FirstOrDefaultAsync(r => r.Id == roleId);

    if (role is null)
      return Result<RoleDto>.NotFound();

    return Result<RoleDto>.Success(new RoleDto(
      role.Id, role.Name!, role.Description, role.IsActive, role.UserRoles.Count, role.CreatedAt));
  }

  public async Task<Result> CreateRoleAsync(string name, string? description)
  {
    var existing = await _roleManager.FindByNameAsync(name);
    if (existing is not null)
      return Result.Conflict($"Role '{name}' already exists.");

    var role = ApplicationRole.Create(name, description);
    var result = await _roleManager.CreateAsync(role);
    if (!result.Succeeded)
      return Result.Error(string.Join("; ", result.Errors.Select(e => e.Description)));

    _logger.LogInformation("Role created: {RoleName}", name);
    return Result.Success();
  }

  public async Task<Result> UpdateRoleAsync(Guid roleId, string name, string? description)
  {
    var role = await _roleManager.FindByIdAsync(roleId.ToString());
    if (role is null)
      return Result.NotFound();

    if (!string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase))
    {
      var existing = await _roleManager.FindByNameAsync(name);
      if (existing is not null)
        return Result.Conflict($"Role '{name}' already exists.");
    }

    role.Name = name;
    role.NormalizedName = name.ToUpperInvariant();
    role.UpdateDescription(description);

    var result = await _roleManager.UpdateAsync(role);
    if (!result.Succeeded)
      return Result.Error(string.Join("; ", result.Errors.Select(e => e.Description)));

    _logger.LogInformation("Role updated: {RoleId} → {RoleName}", roleId, name);
    return Result.Success();
  }

  public async Task<Result> DeleteRoleAsync(Guid roleId)
  {
    var role = await _roleManager.Roles
      .Include(r => r.UserRoles)
      .FirstOrDefaultAsync(r => r.Id == roleId);

    if (role is null)
      return Result.NotFound();

    if (role.UserRoles.Count > 0)
      return Result.Conflict(
        $"Cannot delete role '{role.Name}': {role.UserRoles.Count} user(s) are still assigned to it.");

    var result = await _roleManager.DeleteAsync(role);
    if (!result.Succeeded)
      return Result.Error(string.Join("; ", result.Errors.Select(e => e.Description)));

    _logger.LogInformation("Role deleted: {RoleName}", role.Name);
    return Result.Success();
  }

  // ===== Role Permissions =====

  public async Task<Result<List<RolePermissionDto>>> GetRolePermissionsAsync(Guid roleId)
  {
    var role = await _roleManager.FindByIdAsync(roleId.ToString());
    if (role is null)
      return Result<List<RolePermissionDto>>.NotFound();

    var assigned = await _identityDb.RoleClaims
      .Where(rc => rc.RoleId == roleId && rc.ClaimType == "permission")
      .Select(rc => rc.ClaimValue!)
      .ToHashSetAsync();

    var result = PermissionRegistry.All
      .Select(kv => new RolePermissionDto(kv.Key, kv.Value, assigned.Contains(kv.Key)))
      .OrderBy(p => p.Value)
      .ToList();

    return Result<List<RolePermissionDto>>.Success(result);
  }

  public async Task<Result> SetRolePermissionsAsync(Guid roleId, IList<string> permissions)
  {
    var role = await _roleManager.FindByIdAsync(roleId.ToString());
    if (role is null)
      return Result.NotFound();

    var unknown = permissions.Where(p => !PermissionRegistry.All.ContainsKey(p)).ToList();
    if (unknown.Count > 0)
      return Result.Invalid(new ValidationError("permissions",
        $"Unknown permissions: {string.Join(", ", unknown)}"));

    // Remove all existing permission claims
    var existing = await _identityDb.RoleClaims
      .Where(rc => rc.RoleId == roleId && rc.ClaimType == "permission")
      .ToListAsync();
    _identityDb.RoleClaims.RemoveRange(existing);

    // Add new permission claims with description from registry
    foreach (var perm in permissions.Distinct())
    {
      _identityDb.RoleClaims.Add(new ApplicationRoleClaim
      {
        RoleId      = roleId,
        ClaimType   = "permission",
        ClaimValue  = perm,
        Description = PermissionRegistry.GetDescription(perm)
      });
    }

    await _identityDb.SaveChangesAsync();
    _logger.LogInformation("Permissions updated for role {RoleId}: [{Permissions}]",
      roleId, string.Join(", ", permissions));

    return Result.Success();
  }

  // ===== Private Helpers =====

  private async Task<RefreshToken> IssueRefreshTokenAsync(Guid userId)
  {
    // Clean up expired tokens for this user
    var expiredTokens = await _identityDb.RefreshTokens
      .Where(t => t.UserId == userId && t.ExpiresAt <= DateTime.UtcNow)
      .ToListAsync();
    _identityDb.RefreshTokens.RemoveRange(expiredTokens);

    var token = new RefreshToken
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Token = _jwtService.GenerateRefreshToken(),
      CreatedAt = DateTime.UtcNow,
      ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
      IsRevoked = false
    };

    _identityDb.RefreshTokens.Add(token);
    await _identityDb.SaveChangesAsync();

    return token;
  }

  private async Task RevokeAllUserTokensAsync(Guid userId)
  {
    var activeTokens = await _identityDb.RefreshTokens
      .Where(t => t.UserId == userId && !t.IsRevoked)
      .ToListAsync();

    foreach (var token in activeTokens)
      token.Revoke();

    await _identityDb.SaveChangesAsync();

    _logger.LogInformation("Revoked {Count} refresh tokens for user {UserId}", activeTokens.Count, userId);
  }

  private async Task<IList<string>> GetUserPermissionsAsync(IList<string> roles)
  {
    var permissions = new List<string>();

    foreach (var roleName in roles)
    {
      var role = await _roleManager.FindByNameAsync(roleName);
      if (role is null) continue;

      var claims = await _roleManager.GetClaimsAsync(role);
      permissions.AddRange(
        claims
          .Where(c => c.Type == "permission")
          .Select(c => c.Value));
    }

    return permissions.Distinct().ToList();
  }

  private static string GenerateTemporaryPassword()
  {
    const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string lower = "abcdefghijklmnopqrstuvwxyz";
    const string digits = "0123456789";
    const string all = upper + lower + digits;

    var bytes = new byte[8];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(bytes);

    // Ensure at least one uppercase, one lowercase, one digit
    var result = new StringBuilder();
    result.Append(upper[bytes[0] % upper.Length]);
    result.Append(lower[bytes[1] % lower.Length]);
    result.Append(digits[bytes[2] % digits.Length]);

    for (int i = 3; i < 8; i++)
      result.Append(all[bytes[i] % all.Length]);

    // Shuffle
    var chars = result.ToString().ToCharArray();
    var shuffleBytes = new byte[chars.Length];
    rng.GetBytes(shuffleBytes);
    return new string(chars.OrderBy(_ => shuffleBytes[Array.IndexOf(chars, _)]).ToArray());
  }
}
