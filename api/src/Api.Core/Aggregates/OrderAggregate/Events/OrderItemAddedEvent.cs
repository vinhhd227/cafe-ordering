namespace Api.Core.Aggregates.OrderAggregate.Events;

public class OrderItemAddedEvent : DomainEventBase
{
  public OrderItemAddedEvent(Order order, int productId, int quantity)
  {
    Order = order;
    ProductId = productId;
    Quantity = quantity;
  }

  public Order Order { get; }
  public int ProductId { get; }
  public int Quantity { get; }
}
