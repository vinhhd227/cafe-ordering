using System.Security.Claims;

namespace Api.Web.Extensions;

/// <summary>
/// Extension methods for extracting typed values from ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
  /// <summary>
  /// Get ApplicationUser ID (int) from claims
  /// </summary>
  public static int GetUserId(this ClaimsPrincipal principal)
  {
    var value = principal.FindFirstValue("userId");
    return value is not null
      ? int.Parse(value)
      : throw new UnauthorizedAccessException("User ID not found in claims");
  }

  /// <summary>
  /// Get Customer ID (string) from claims
  /// </summary>
  public static string GetCustomerId(this ClaimsPrincipal principal)
  {
    var value = principal.FindFirstValue("customerId");
    return value ?? throw new UnauthorizedAccessException("Customer ID not found in claims");
  }

  /// <summary>
  /// Get Customer ID (string, nullable) from claims
  /// </summary>
  public static string? GetCustomerIdOrNull(this ClaimsPrincipal principal)
  {
    return principal.FindFirstValue("customerId");
  }

  /// <summary>
  /// Check if user has Admin role
  /// </summary>
  public static bool IsAdmin(this ClaimsPrincipal principal)
    => principal.IsInRole("Admin");

  /// <summary>
  /// Check if user has Staff or Admin role
  /// </summary>
  public static bool IsStaff(this ClaimsPrincipal principal)
    => principal.IsInRole("Staff") || principal.IsInRole("Admin");

  /// <summary>
  /// Check if user has Customer role
  /// </summary>
  public static bool IsCustomer(this ClaimsPrincipal principal)
    => principal.IsInRole("Customer");
}
