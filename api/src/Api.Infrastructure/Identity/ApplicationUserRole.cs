using Microsoft.AspNetCore.Identity;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Many-to-many relationship between Users and Roles.
/// </summary>
public class ApplicationUserRole : IdentityUserRole<Guid>
{
  public ApplicationUser User { get; set; } = null!;
  public ApplicationRole Role { get; set; } = null!;
}
