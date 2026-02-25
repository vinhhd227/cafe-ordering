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
  public string? CustomerId { get; private set; }  // FK to Customer.Id (nullable — guest orders have no customer)
  public Guid SessionId { get; private set; }       // FK to GuestSession.Id (required)
  public string? DeviceToken { get; private set; }  // Anonymous device token from client
  public OrderStatus Status { get; private set; }
  public DateTime OrderDate { get; private set; }
  public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

  // Calculated
  public decimal TotalAmount => _items.Sum(i => i.TotalPrice);

  /// <summary>
  ///   Factory method for session-based orders (guest or authenticated).
  /// </summary>
  public static Order Create(Guid sessionId, string orderNumber, string? deviceToken = null, string? customerId = null)
  {
    Guard.Against.Default(sessionId, nameof(sessionId));
    Guard.Against.NullOrEmpty(orderNumber, nameof(orderNumber));

    var order = new Order
    {
      SessionId = sessionId,
      OrderNumber = orderNumber,
      DeviceToken = deviceToken,
      CustomerId = customerId,
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

  public void Process()
  {
    if (!Status.CanTransitionTo(OrderStatus.Processing))
      throw new InvalidOperationException($"Cannot process order in {Status} status");

    Status = OrderStatus.Processing;
  }

  public void Cancel()
  {
    if (!Status.CanTransitionTo(OrderStatus.Cancelled))
      throw new InvalidOperationException($"Cannot cancel order in {Status} status");

    Status = OrderStatus.Cancelled;
  }

  public void Complete()
  {
    if (!Status.CanTransitionTo(OrderStatus.Completed))
      throw new InvalidOperationException($"Cannot complete order in {Status} status");

    Status = OrderStatus.Completed;

    RegisterDomainEvent(new OrderCompletedEvent(this));
  }
}
