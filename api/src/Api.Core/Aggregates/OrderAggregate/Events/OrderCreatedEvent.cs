namespace Api.Core.Aggregates.OrderAggregate.Events;

public class OrderCreatedEvent : DomainEventBase
{
  public OrderCreatedEvent(Order order)
  {
    Order = order;
  }

  public Order Order { get; }
}
