using Api.Core.Events.Identity;
using Microsoft.AspNetCore.Identity;

namespace Api.Core.Entities.Identity;

/// <summary>
/// Application user entity extending IdentityUser.
/// Uses int as primary key to align with existing entity patterns.
/// Includes audit tracking and domain logic.
/// </summary>
public class ApplicationUser : IdentityUser<int>, IAggregateRoot
{
  // === Audit Properties (from AuditableEntity pattern) ===

  public DateTime CreatedAt { get; set; }
  public string? CreatedBy { get; set; }
  public DateTime UpdatedAt { get; set; }
  public string? UpdatedBy { get; set; }
  public byte[] RowVersion { get; set; } = Array.Empty<byte>();

  // === Custom Application Properties ===

  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public bool IsActive { get; set; } = true;
  public string? RefreshToken { get; set; }
  public DateTime? RefreshTokenExpiryTime { get; set; }

  // === Navigation Properties ===

  public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();

  // Optional: Link to Customer aggregate (for cafe ordering domain)
  public string? CustomerId { get; set; } // FK to Customer.Id (string)

  // === Domain Events (from BaseEntity pattern) ===

  [System.ComponentModel.DataAnnotations.Schema.NotMapped]
  private readonly List<DomainEventBase> _domainEvents = new();

  [System.ComponentModel.DataAnnotations.Schema.NotMapped]
  public IReadOnlyCollection<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();

  protected void RegisterDomainEvent(DomainEventBase domainEvent)
  {
    _domainEvents.Add(domainEvent);
  }

  public void ClearDomainEvents()
  {
    _domainEvents.Clear();
  }

  // === Computed Properties ===

  public string FullName => $"{FirstName} {LastName}";

  // === Factory Method ===

  /// <summary>
  /// Create a new user
  /// </summary>
  public static ApplicationUser Create(
    string userName,
    string email,
    string firstName,
    string lastName)
  {
    Guard.Against.NullOrEmpty(userName, nameof(userName));
    Guard.Against.NullOrEmpty(email, nameof(email));
    Guard.Against.NullOrEmpty(firstName, nameof(firstName));
    Guard.Against.NullOrEmpty(lastName, nameof(lastName));

    var user = new ApplicationUser
    {
      UserName = userName,
      Email = email,
      FirstName = firstName,
      LastName = lastName,
      EmailConfirmed = false,
      LockoutEnabled = true,
      SecurityStamp = Guid.NewGuid().ToString(),
      ConcurrencyStamp = Guid.NewGuid().ToString()
    };

    user.RegisterDomainEvent(new UserCreatedEvent(user));
    return user;
  }

  // === Business Methods ===

  /// <summary>
  /// Update user profile information
  /// </summary>
  public void UpdateProfile(string firstName, string lastName, string? phoneNumber)
  {
    Guard.Against.NullOrEmpty(firstName, nameof(firstName));
    Guard.Against.NullOrEmpty(lastName, nameof(lastName));

    FirstName = firstName;
    LastName = lastName;
    PhoneNumber = phoneNumber;
  }

  /// <summary>
  /// Confirm user email
  /// </summary>
  public void ConfirmEmail()
  {
    EmailConfirmed = true;
    RegisterDomainEvent(new UserEmailConfirmedEvent(Id, Email!));
  }

  /// <summary>
  /// Link this user to a Customer aggregate
  /// </summary>
  public void LinkToCustomer(string customerId)
  {
    Guard.Against.NullOrEmpty(customerId, nameof(customerId));

    CustomerId = customerId;
    RegisterDomainEvent(new UserLinkedToCustomerEvent(Id, customerId));
  }

  /// <summary>
  /// Deactivate user account
  /// </summary>
  public void Deactivate()
  {
    IsActive = false;
    LockoutEnd = DateTimeOffset.MaxValue;
  }

  /// <summary>
  /// Activate user account
  /// </summary>
  public void Activate()
  {
    IsActive = true;
    LockoutEnd = null;
  }

  /// <summary>
  /// Set refresh token for JWT authentication
  /// </summary>
  public void SetRefreshToken(string refreshToken, DateTime expiryTime)
  {
    Guard.Against.NullOrEmpty(refreshToken, nameof(refreshToken));

    RefreshToken = refreshToken;
    RefreshTokenExpiryTime = expiryTime;
  }

  /// <summary>
  /// Clear refresh token
  /// </summary>
  public void ClearRefreshToken()
  {
    RefreshToken = null;
    RefreshTokenExpiryTime = null;
  }
}
