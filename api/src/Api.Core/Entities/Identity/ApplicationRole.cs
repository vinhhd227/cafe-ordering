using Microsoft.AspNetCore.Identity;

namespace Api.Core.Entities.Identity;

/// <summary>
/// Application role entity extending IdentityRole.
/// Uses int as primary key for consistency.
/// </summary>
public class ApplicationRole : IdentityRole<int>
{
  public string? Description { get; set; }
  public bool IsActive { get; set; } = true;

  // Navigation
  public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();

  /// <summary>
  /// Create a new role
  /// </summary>
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

  /// <summary>
  /// Update role description
  /// </summary>
  public void UpdateDescription(string? description)
  {
    Description = description;
  }

  /// <summary>
  /// Deactivate role
  /// </summary>
  public void Deactivate()
  {
    IsActive = false;
  }

  /// <summary>
  /// Activate role
  /// </summary>
  public void Activate()
  {
    IsActive = true;
  }
}
