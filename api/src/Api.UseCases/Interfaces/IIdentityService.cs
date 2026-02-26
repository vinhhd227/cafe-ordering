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

  /// <summary>
  /// Get a paged, filterable list of all users for admin management.
  /// </summary>
  Task<Result<PagedUsersDto>> GetUsersAsync(int page, int pageSize, string? search, string? role, bool? isActive);

  /// <summary>
  /// Update a user's FullName and Email.
  /// </summary>
  Task<Result> UpdateUserAsync(Guid userId, string fullName, string? email);

  /// <summary>
  /// Re-activate a previously deactivated user account.
  /// </summary>
  Task<Result> ActivateUserAsync(Guid userId);

  /// <summary>
  /// Replace all current roles of a user with a single new role.
  /// </summary>
  Task<Result> ChangeUserRoleAsync(Guid userId, string newRole);

  /// <summary>
  /// Get a single user by ID for the admin detail view.
  /// </summary>
  Task<Result<UserDto>> GetUserByIdAsync(Guid userId);

  // ===== Role Management =====

  /// <summary>Get a paged, searchable list of all roles.</summary>
  Task<Result<PagedRolesDto>> GetRolesAsync(int page, int pageSize, string? search);

  /// <summary>Get a single role by ID.</summary>
  Task<Result<RoleDto>> GetRoleByIdAsync(Guid roleId);

  /// <summary>Create a new role. Returns Conflict if name already exists.</summary>
  Task<Result> CreateRoleAsync(string name, string? description);

  /// <summary>Update a role's name and description.</summary>
  Task<Result> UpdateRoleAsync(Guid roleId, string name, string? description);

  /// <summary>Delete a role. Returns Conflict if users are still assigned.</summary>
  Task<Result> DeleteRoleAsync(Guid roleId);

  // ===== Role Permissions =====

  /// <summary>
  /// Get all known permissions with their descriptions,
  /// flagging which ones are currently assigned to the given role.
  /// </summary>
  Task<Result<List<RolePermissionDto>>> GetRolePermissionsAsync(Guid roleId);

  /// <summary>
  /// Replace all permission claims on a role with the provided set.
  /// Unknown permission values are rejected with Invalid result.
  /// </summary>
  Task<Result> SetRolePermissionsAsync(Guid roleId, IList<string> permissions);
}

/// <summary>Response returned after a successful login or token refresh.</summary>
public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);

/// <summary>Response returned after creating a staff account.</summary>
public record TemporaryPasswordDto(string Username, string TemporaryPassword);

/// <summary>Represents a user record in admin user management.</summary>
public record UserDto(
  Guid Id,
  string Username,
  string FullName,
  string? Email,
  List<string> Roles,
  bool IsActive,
  DateTime CreatedAt);

/// <summary>Paged result for user list queries.</summary>
public record PagedUsersDto(List<UserDto> Items, int Total, int Page, int PageSize);

/// <summary>Represents a role record in admin role management.</summary>
public record RoleDto(Guid Id, string Name, string? Description, bool IsActive, int UserCount);

/// <summary>Paged result for role list queries.</summary>
public record PagedRolesDto(List<RoleDto> Items, int Total, int Page, int PageSize);

/// <summary>
/// A permission entry as shown in the role permissions editor.
/// Contains the value, its human-readable description, and whether it is currently assigned.
/// </summary>
public record RolePermissionDto(string Value, string Description, bool Assigned);
