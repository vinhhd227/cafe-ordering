namespace Api.Core.Aggregates.CustomerAggregate.Events;

/// <summary>
/// Domain event raised when a customer's email address is changed.
/// Used to synchronize email with ApplicationUser in the Identity system.
/// </summary>
public class CustomerEmailChangedEvent(string customerId, string oldEmail, string newEmail)
  : DomainEventBase
{
  public string CustomerId { get; } = customerId;
  public string OldEmail { get; } = oldEmail;
  public string NewEmail { get; } = newEmail;
}
