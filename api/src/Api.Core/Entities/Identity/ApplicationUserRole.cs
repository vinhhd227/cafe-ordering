using Microsoft.AspNetCore.Identity;

namespace Api.Core.Entities.Identity;

/// <summary>
/// Many-to-many relationship between Users and Roles
/// </summary>
public class ApplicationUserRole : IdentityUserRole<int>
{
  public ApplicationUser User { get; set; } = null!;
  public ApplicationRole Role { get; set; } = null!;
}
