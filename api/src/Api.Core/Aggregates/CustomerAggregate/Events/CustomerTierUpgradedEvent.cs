namespace Api.Core.Aggregates.CustomerAggregate.Events;

/// <summary>
/// Domain event raised when a customer's tier is upgraded.
/// </summary>
public class CustomerTierUpgradedEvent : DomainEventBase
{
  public CustomerTierUpgradedEvent(Customer customer, CustomerTier oldTier, CustomerTier newTier)
  {
    CustomerId = customer.Id;  // Customer.Id is string
    OldTier = oldTier;
    NewTier = newTier;
  }

  public string CustomerId { get; }
  public CustomerTier OldTier { get; }
  public CustomerTier NewTier { get; }
}
