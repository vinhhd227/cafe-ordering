// Core/Aggregates/OrderAggregate/Order.cs

using Api.Core.Aggregates.OrderAggregate.Events;
using Api.Core.Domain.Enums;

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
  public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Unpaid;
  public PaymentMethod PaymentMethod { get; private set; } = PaymentMethod.Unknown;
  public decimal? AmountReceived { get; private set; }
  public decimal TipAmount { get; private set; }
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

  public void UpdatePayment(PaymentStatus status, PaymentMethod method,
    decimal? amountReceived = null, decimal tipAmount = 0)
  {
    if (status == PaymentStatus.Paid && method == PaymentMethod.Unknown)
      throw new InvalidOperationException("PaymentMethod is required when marking as Paid.");

    PaymentStatus = status;
    PaymentMethod = method;
    AmountReceived = amountReceived;
    TipAmount = tipAmount;
  }

  /// <summary>
  ///   Merge: thêm item bất kể status (bypass CanAddItems guard).
  ///   Nếu cùng productId → cộng dồn quantity.
  /// </summary>
  public void AddItemForMerge(int productId, string productName, decimal unitPrice, int quantity)
  {
    var existing = _items.FirstOrDefault(i => i.ProductId == productId);
    if (existing != null)
      existing.UpdateQuantity(existing.Quantity + quantity);
    else
      _items.Add(OrderItem.Create(Id, productId, productName, unitPrice, quantity));
  }

  /// <summary>
  ///   Split: xoá hoặc giảm quantity của một item.
  /// </summary>
  public void RemoveItem(int productId, int quantity)
  {
    var item = _items.FirstOrDefault(i => i.ProductId == productId);
    if (item is null)
      throw new InvalidOperationException($"Product {productId} not found in order.");
    if (quantity >= item.Quantity)
      _items.Remove(item);
    else
      item.UpdateQuantity(item.Quantity - quantity);
  }

  /// <summary>
  ///   Merge: cancel secondary order bất kể status.CanCancel.
  /// </summary>
  public void CancelAsMerged()
  {
    Status = OrderStatus.Cancelled;
  }

  /// <summary>
  ///   Set the quantity of an item. quantity = 0 removes the item.
  ///   Adds a new item if productId not found in the order.
  ///   Only allowed on Pending orders.
  /// </summary>
  public void SetItemQuantity(int productId, string productName, decimal unitPrice, int quantity)
  {
    if (!Status.CanAddItems)
      throw new InvalidOperationException("Can only edit items on Pending orders.");

    var existing = _items.FirstOrDefault(i => i.ProductId == productId);

    if (quantity <= 0)
    {
      if (existing is not null) _items.Remove(existing);
      return;
    }

    if (existing is not null)
      existing.UpdateQuantity(quantity);
    else
      _items.Add(OrderItem.Create(Id, productId, productName, unitPrice, quantity));
  }
}
