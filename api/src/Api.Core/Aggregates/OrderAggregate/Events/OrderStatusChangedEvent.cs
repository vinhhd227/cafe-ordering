namespace Api.Core.Aggregates.OrderAggregate.Events;

public class OrderStatusChangedEvent : DomainEventBase
{
  public OrderStatusChangedEvent(Order order) => Order = order;

  public Order Order { get; }
}
