namespace Api.Core.Aggregates.OrderAggregate.Events;

public class OrderCompletedEvent : DomainEventBase
{
  public OrderCompletedEvent(Order order)
  {
    Order = order;
  }

  public Order Order { get; }
}
