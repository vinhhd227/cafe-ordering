// Core/Aggregates/OrderAggregate/Order.cs

using Api.Core.Aggregates.OrderAggregate.Events;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Exceptions;

namespace Api.Core.Aggregates.OrderAggregate;

/// <summary>
///   Order Aggregate Root - sử dụng int Id
/// </summary>
public class Order : AuditableEntity<int>, IAggregateRoot
{
  // Navigation
  private readonly List<OrderItem> _items = new();
  private readonly List<OrderPromotion> _promotions = new();

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
  public int? GuestCount { get; private set; }
  public decimal? AmountReceived { get; private set; }
  public decimal TipAmount { get; private set; }
  public DateTime OrderDate { get; private set; }
  public DateTime? CompletedAt { get; private set; }
  public DateTime? PaidAt { get; private set; }
  public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
  public IReadOnlyCollection<OrderPromotion> Promotions => _promotions.AsReadOnly();

  // Calculated
  public decimal TotalAmount  => _items.Sum(i => i.TotalPrice);
  public decimal TotalDiscount => _promotions.Sum(p => p.DiscountAmount);
  public decimal FinalAmount   => Math.Max(0, TotalAmount - TotalDiscount);

  /// <summary>
  ///   Factory method for session-based orders (guest or authenticated).
  /// </summary>
  public static Order Create(Guid sessionId, string orderNumber, string? deviceToken = null, string? customerId = null, int? guestCount = null)
  {
    Guard.Against.Default(sessionId, nameof(sessionId));
    Guard.Against.NullOrEmpty(orderNumber, nameof(orderNumber));

    var order = new Order
    {
      SessionId = sessionId,
      OrderNumber = orderNumber,
      DeviceToken = deviceToken,
      CustomerId = customerId,
      GuestCount = guestCount,
      Status = OrderStatus.Pending,
      OrderDate = DateTime.UtcNow
    };

    return order;
  }

  /// <summary>
  ///   Đăng ký OrderCreatedEvent — gọi SAU khi items đã được thêm vào order
  ///   để SSE broadcast có đầy đủ thông tin (items, totalAmount).
  /// </summary>
  public void NotifyCreated() => RegisterDomainEvent(new OrderCreatedEvent(this));

  public void AddItem(int productId, string productName, decimal unitPrice, int quantity,
    DrinkTemperature? temperature = null, IceLevel? iceLevel = null, SugarLevel? sugarLevel = null,
    bool isTakeaway = false, bool isFreeGift = false)
  {
    if (Status == OrderStatus.Completed)
    {
      throw new InvalidOperationException("Cannot add items to completed order");
    }

    var item = OrderItem.Create(Id, productId, productName, unitPrice, quantity, temperature, iceLevel, sugarLevel, isTakeaway, isFreeGift);
    _items.Add(item);

    RegisterDomainEvent(new OrderItemAddedEvent(this, productId, quantity));
  }

  public void Process()
  {
    if (!Status.CanTransitionTo(OrderStatus.Processing))
      throw new InvalidOperationException($"Cannot process order in {Status} status");

    Status = OrderStatus.Processing;
    RegisterDomainEvent(new OrderStatusChangedEvent(this));
  }

  public void Cancel()
  {
    if (!Status.CanTransitionTo(OrderStatus.Cancelled))
      throw new InvalidOperationException($"Cannot cancel order in {Status} status");

    Status = OrderStatus.Cancelled;
    RegisterDomainEvent(new OrderStatusChangedEvent(this));
  }

  public void Complete()
  {
    if (!Status.CanTransitionTo(OrderStatus.Completed))
      throw new InvalidOperationException($"Cannot complete order in {Status} status");

    Status = OrderStatus.Completed;
    CompletedAt = DateTime.UtcNow;

    RegisterDomainEvent(new OrderCompletedEvent(this));
    RegisterDomainEvent(new OrderStatusChangedEvent(this));
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
    if (status == PaymentStatus.Paid) PaidAt ??= DateTime.UtcNow;

    RegisterDomainEvent(new OrderPaymentUpdatedEvent(this));
  }

  // ── Promotion methods ────────────────────────────────────────────

  /// <summary>
  ///   Áp dụng một chương trình khuyến mãi vào order.
  ///   Mỗi order chỉ được dùng 1 voucher. Chỉ áp dụng được khi order đang PENDING.
  /// </summary>
  public void ApplyPromotion(int promotionId, string promoCode, decimal discountAmount)
  {
    if (Status != OrderStatus.Pending)
      throw new DomainException("Promotions can only be applied to Pending orders.");

    if (_promotions.Any())
      throw new DomainException("Order already has a promotion applied. Remove it before applying another.");

    _promotions.Add(OrderPromotion.Create(Id, promotionId, promoCode, discountAmount));
    RegisterDomainEvent(new OrderPromotionAppliedEvent(this, promotionId));
  }

  /// <summary>
  ///   Xóa một chương trình khuyến mãi khỏi order.
  ///   Đồng thời reset item-level discounts liên quan về 0.
  ///   Chỉ được phép khi order đang PENDING.
  /// </summary>
  public void RemovePromotion(int promotionId)
  {
    if (Status != OrderStatus.Pending)
      throw new DomainException("Promotions can only be removed from Pending orders.");

    var promo = _promotions.FirstOrDefault(p => p.PromotionId == promotionId);
    if (promo is null) return;

    _promotions.Remove(promo);
    RegisterDomainEvent(new OrderPromotionRemovedEvent(this, promotionId));
  }

  /// <summary>
  ///   Xóa toàn bộ free gift items khỏi order.
  ///   Gọi khi một item thường bị xóa — điều kiện khuyến mãi có thể không còn thỏa.
  /// </summary>
  public void RemoveFreeGiftItems()
  {
    _items.RemoveAll(i => i.IsFreeGift);
  }

  /// <summary>
  ///   Reset tất cả item discounts về 0. Gọi khi xóa một promo có PRODUCT/CATEGORY scope.
  /// </summary>
  public void ResetAllItemDiscounts()
  {
    foreach (var item in _items)
      item.ApplyDiscount(0);
  }

  // ── Merge/Split methods ──────────────────────────────────────────

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
  ///   Merge: cộng dồn số khách từ secondary order vào primary.
  ///   Chỉ cộng nếu secondary có GuestCount (không null).
  /// </summary>
  public void AddGuestCount(int? secondaryGuestCount)
  {
    if (secondaryGuestCount is null) return;
    GuestCount = (GuestCount ?? 0) + secondaryGuestCount.Value;
  }

  /// <summary>
  ///   Edit: xoá toàn bộ items + promotions để replace bằng danh sách mới.
  /// </summary>
  public void ClearAllItems()
  {
    _items.Clear();
    _promotions.Clear();
  }

  /// <summary>
  ///   Cập nhật số khách (dùng khi edit order).
  /// </summary>
  public void UpdateGuestCount(int? value) => GuestCount = value;

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
