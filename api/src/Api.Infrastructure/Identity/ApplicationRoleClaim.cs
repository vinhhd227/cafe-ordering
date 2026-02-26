using Microsoft.AspNetCore.Identity;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Extends IdentityRoleClaim with a human-readable Description
/// that explains what this permission allows the user to do.
/// </summary>
public class ApplicationRoleClaim : IdentityRoleClaim<Guid>
{
  /// <summary>Human-readable description of what this claim/permission does.</summary>
  public string? Description { get; set; }
}
