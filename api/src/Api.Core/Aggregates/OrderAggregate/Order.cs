// Core/Aggregates/OrderAggregate/Order.cs

using Api.Core.Aggregates.OrderAggregate.Events;

namespace Api.Core.Aggregates.OrderAggregate;

/// <summary>
///   Order Aggregate Root - sử dụng int Id
/// </summary>
public class Order : AuditableEntity<int>, IAggregateRoot
{
  // Navigation
  private readonly List<OrderItem> _items = new();

  // Private constructor
  private Order() { }

  // Properties
  public string OrderNumber { get; private set; } = string.Empty;
  public string CustomerId { get; private set; } = string.Empty;  // FK to Customer.Id (string)
  public OrderStatus Status { get; private set; }
  public DateTime OrderDate { get; private set; }
  public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

  // Calculated
  public decimal TotalAmount => _items.Sum(i => i.TotalPrice);

  /// <summary>
  ///   Factory method
  /// </summary>
  public static Order Create(string customerId, string orderNumber)
  {
    Guard.Against.NullOrEmpty(customerId, nameof(customerId));
    Guard.Against.NullOrEmpty(orderNumber, nameof(orderNumber));

    var order = new Order
    {
      CustomerId = customerId,
      OrderNumber = orderNumber,
      Status = OrderStatus.Pending,
      OrderDate = DateTime.UtcNow
    };

    // Register domain event
    order.RegisterDomainEvent(new OrderCreatedEvent(order));

    return order;
  }

  public void AddItem(int productId, string productName, decimal unitPrice, int quantity)
  {
    if (Status == OrderStatus.Completed)
    {
      throw new InvalidOperationException("Cannot add items to completed order");
    }

    var item = OrderItem.Create(Id, productId, productName, unitPrice, quantity);
    _items.Add(item);

    RegisterDomainEvent(new OrderItemAddedEvent(this, productId, quantity));
  }

  public void Complete()
  {
    if (Status != OrderStatus.Processing)
    {
      throw new InvalidOperationException($"Cannot complete order in {Status} status");
    }

    Status = OrderStatus.Completed;

    RegisterDomainEvent(new OrderCompletedEvent(this));
  }
}
