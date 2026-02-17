using Api.Core.Entities.Identity;
using Api.UseCases.Interfaces;
using Ardalis.Result;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Implementation of IIdentityService using ASP.NET Core Identity.
/// Handles user authentication and management.
/// </summary>
public class IdentityService : IIdentityService
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly IJwtService _jwtService;
  private readonly ILogger<IdentityService> _logger;

  public IdentityService(
    UserManager<ApplicationUser> userManager,
    IJwtService jwtService,
    ILogger<IdentityService> logger)
  {
    _userManager = userManager;
    _jwtService = jwtService;
    _logger = logger;
  }

  public async Task<Result> CreateUserAsync(string email, string password, string customerId)
  {
    var user = new ApplicationUser
    {
      UserName = email,
      Email = email,
      CustomerId = customerId,  // string FK to Customer.Id
      EmailConfirmed = false,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    var result = await _userManager.CreateAsync(user, password);
    if (!result.Succeeded)
    {
      var errors = result.Errors.Select(e => e.Description).ToArray();
      return Result.Error(errors);
    }

    // Assign default "Customer" role
    await _userManager.AddToRoleAsync(user, "Customer");

    _logger.LogInformation("User created for customer {CustomerId}", customerId);
    return Result.Success();
  }

  public async Task<Result<TokenResponse>> LoginAsync(string email, string password)
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

    // Get roles for JWT
    var roles = await _userManager.GetRolesAsync(user);

    // Generate tokens
    var accessToken = _jwtService.GenerateAccessToken(
      userId: user.Id,
      customerId: user.CustomerId,
      email: user.Email!,
      roles: roles);

    var refreshToken = _jwtService.GenerateRefreshToken();
    user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
    user.UpdatedAt = DateTime.UtcNow;

    await _userManager.UpdateAsync(user);

    return Result<TokenResponse>.Success(new TokenResponse(
      accessToken,
      refreshToken,
      ExpiresIn: 3600));
  }

  public async Task<Result> UpdateEmailAsync(string customerId, string newEmail)
  {
    var user = await _userManager.Users
      .FirstOrDefaultAsync(u => u.CustomerId == customerId);  // string FK

    if (user is null)
      return Result.NotFound("User not found for customer");

    user.Email = newEmail;
    user.UserName = newEmail;
    user.EmailConfirmed = false; // Require re-confirmation
    user.UpdatedAt = DateTime.UtcNow;

    var result = await _userManager.UpdateAsync(user);
    return result.Succeeded
      ? Result.Success()
      : Result.Error(result.Errors.Select(e => e.Description).ToArray());
  }

  public async Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
  {
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user is null)
      return Result.NotFound();

    var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    return result.Succeeded
      ? Result.Success()
      : Result.Error(result.Errors.Select(e => e.Description).ToArray());
  }

  public async Task<Result> DeactivateUserAsync(string customerId)
  {
    var user = await _userManager.Users
      .FirstOrDefaultAsync(u => u.CustomerId == customerId);  // string FK

    if (user is null)
      return Result.NotFound();

    user.Deactivate();
    user.UpdatedAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    return Result.Success();
  }

  public async Task<Result> ResetPasswordAsync(string email)
  {
    var user = await _userManager.FindByEmailAsync(email);
    if (user is null)
      return Result.Success(); // Don't reveal user existence

    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
    // TODO: Send email with token
    _logger.LogInformation("Password reset token generated for {Email}", email);

    return Result.Success();
  }

  public async Task<Result<TokenResponse>> RefreshTokenAsync(string accessToken, string refreshToken)
  {
    // Validate refresh token and generate new tokens
    // Implementation details...
    throw new NotImplementedException("RefreshTokenAsync not yet implemented");
  }
}
