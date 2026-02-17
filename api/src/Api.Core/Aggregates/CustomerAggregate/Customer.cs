using Api.Core.Aggregates.CustomerAggregate.Events;

namespace Api.Core.Aggregates.CustomerAggregate;

/// <summary>
///   Customer Aggregate Root - sử dụng string Id (từ external system)
/// </summary>
public class Customer : AuditableEntity<string>, IAggregateRoot
{
  // Private constructor
  private Customer() { }

  // Personal data - OWNED BY CUSTOMER
  public string FirstName { get; private set; } = string.Empty;
  public string LastName { get; private set; } = string.Empty;
  public string Email { get; private set; } = string.Empty;
  public string? PhoneNumber { get; private set; }

  // Business data
  public CustomerTier Tier { get; private set; }

  public string FullName => $"{FirstName} {LastName}";

  /// <summary>
  ///   Factory method - Id từ external system
  /// </summary>
  public static Customer Create(string externalId, string firstName, string lastName, string email)
  {
    var customer = new Customer
    {
      Id = externalId, // External system Id
      FirstName = firstName,
      LastName = lastName,
      Email = email,
      Tier = CustomerTier.Bronze
    };

    customer.RegisterDomainEvent(new CustomerCreatedEvent(customer));

    return customer;
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
}
