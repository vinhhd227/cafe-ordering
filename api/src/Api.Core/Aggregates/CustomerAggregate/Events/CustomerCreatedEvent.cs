namespace Api.Core.Aggregates.CustomerAggregate.Events;

public class CustomerCreatedEvent : DomainEventBase
{
  public CustomerCreatedEvent(Customer customer)
  {
    Customer = customer;
  }

  public Customer Customer { get; }
}
