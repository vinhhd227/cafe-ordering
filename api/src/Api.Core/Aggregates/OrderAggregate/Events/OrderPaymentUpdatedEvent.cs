namespace Api.Core.Aggregates.OrderAggregate.Events;

public class OrderPaymentUpdatedEvent : DomainEventBase
{
  public OrderPaymentUpdatedEvent(Order order) => Order = order;

  public Order Order { get; }
}
