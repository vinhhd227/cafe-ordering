namespace Api.Core.Aggregates.CustomerAggregate.Events;

public class CustomerTierUpgradedEvent : DomainEventBase
{
  public CustomerTierUpgradedEvent(Customer customer, CustomerTier oldTier, CustomerTier newTier)
  {
    Customer = customer;
    OldTier = oldTier;
    NewTier = newTier;
  }

  public Customer Customer { get; }
  public CustomerTier OldTier { get; }
  public CustomerTier NewTier { get; }
}
