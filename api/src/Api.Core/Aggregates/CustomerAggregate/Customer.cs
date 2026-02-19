using Api.Core.Aggregates.CustomerAggregate.Events;

namespace Api.Core.Aggregates.CustomerAggregate;

/// <summary>
/// Customer Aggregate Root.
/// Linked to Identity system via IdentityGuid (no FK constraint).
/// </summary>
public class Customer : AuditableEntity<string>, IAggregateRoot
{
  private Customer() { }

  public string FirstName { get; private set; } = string.Empty;
  public string LastName { get; private set; } = string.Empty;
  public string Email { get; private set; } = string.Empty;
  public string? PhoneNumber { get; private set; }

  /// <summary>
  /// Links to ApplicationUser.Id in Identity DB. No FK — string value only.
  /// </summary>
  public string? IdentityGuid { get; private set; }

  // Soft delete
  public bool IsDeleted { get; private set; }
  public DateTime? DeletedAt { get; private set; }

  public CustomerTier Tier { get; private set; }

  public string FullName => $"{FirstName} {LastName}";

  /// <summary>
  /// Factory method. Id is a new Guid generated internally.
  /// </summary>
  public static Customer Create(string firstName, string lastName, string email)
  {
    var customer = new Customer
    {
      Id = Guid.NewGuid().ToString(),
      FirstName = firstName,
      LastName = lastName,
      Email = email,
      Tier = CustomerTier.Bronze
    };

    customer.RegisterDomainEvent(new CustomerCreatedEvent(customer));

    return customer;
  }

  /// <summary>
  /// Links this customer to an Identity user after successful registration.
  /// </summary>
  public void LinkToIdentity(string identityGuid)
  {
    Guard.Against.NullOrEmpty(identityGuid, nameof(identityGuid));
    IdentityGuid = identityGuid;
  }

  /// <summary>
  /// Update customer profile information
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
  /// Update customer email and trigger sync event
  /// </summary>
  public void UpdateEmail(string newEmail)
  {
    Guard.Against.NullOrEmpty(newEmail, nameof(newEmail));

    var oldEmail = Email;
    Email = newEmail;

    RegisterDomainEvent(new CustomerEmailChangedEvent(Id, oldEmail, newEmail));
  }

  public void UpgradeTier(CustomerTier newTier)
  {
    if (newTier <= Tier)
    {
      throw new InvalidOperationException("Cannot downgrade tier");
    }

    var oldTier = Tier;
    Tier = newTier;

    RegisterDomainEvent(new CustomerTierUpgradedEvent(this, oldTier, newTier));
  }

  /// <summary>
  /// Soft delete — preserves order history. Use instead of hard delete.
  /// </summary>
  public void SoftDelete()
  {
    if (IsDeleted) throw new InvalidOperationException("Customer is already deleted");

    IsDeleted = true;
    DeletedAt = DateTime.UtcNow;
  }

  public void Restore()
  {
    if (!IsDeleted) throw new InvalidOperationException("Customer is not deleted");

    IsDeleted = false;
    DeletedAt = null;
  }
}
