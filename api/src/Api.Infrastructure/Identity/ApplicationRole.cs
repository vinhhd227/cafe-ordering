using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Identity;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Application role entity extending IdentityRole.
/// </summary>
public class ApplicationRole : IdentityRole<int>
{
  public string? Description { get; set; }
  public bool IsActive { get; set; } = true;

  public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();

  public static ApplicationRole Create(string name, string? description = null)
  {
    Guard.Against.NullOrEmpty(name, nameof(name));

    return new ApplicationRole
    {
      Name = name,
      Description = description,
      ConcurrencyStamp = Guid.NewGuid().ToString()
    };
  }

  public void UpdateDescription(string? description)
  {
    Description = description;
  }

  public void Deactivate() => IsActive = false;

  public void Activate() => IsActive = true;
}
