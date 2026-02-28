using Api.Core.Exceptions;

namespace Api.Core.Aggregates.OrderAggregate;

/// <summary>
///   OrderItem là entity BÊN TRONG Order aggregate
///   KHÔNG thể tồn tại độc lập, không phải Aggregate Root
/// </summary>
public class OrderItem : BaseEntity
{
  // Private constructor
  private OrderItem() { }
  public int OrderId { get; private set; }
  public int ProductId { get; private set; }
  public string ProductName { get; private set; } = string.Empty;
  public decimal UnitPrice { get; private set; }
  public int Quantity { get; private set; }
  public decimal Discount { get; private set; }

  // Customization options
  public string? Temperature { get; private set; }
  public string? IceLevel { get; private set; }
  public string? SugarLevel { get; private set; }
  public bool IsTakeaway { get; private set; }

  // Calculated property
  public decimal TotalPrice => (UnitPrice - Discount) * Quantity;

  /// <summary>
  ///   Factory method - chỉ được gọi từ Order aggregate
  /// </summary>
  internal static OrderItem Create(int orderId, int productId,
    string productName, decimal unitPrice, int quantity,
    string? temperature = null, string? iceLevel = null, string? sugarLevel = null,
    bool isTakeaway = false)
  {
    return new OrderItem
    {
      OrderId = Guard.Against.NegativeOrZero(orderId),
      ProductId = Guard.Against.NegativeOrZero(productId),
      ProductName = Guard.Against.NullOrEmpty(productName),
      UnitPrice = Guard.Against.NegativeOrZero(unitPrice),
      Quantity = Guard.Against.NegativeOrZero(quantity),
      Discount = 0,
      Temperature = temperature,
      IceLevel = iceLevel,
      SugarLevel = sugarLevel,
      IsTakeaway = isTakeaway,
    };
  }

  /// <summary>
  ///   Internal methods - chỉ được gọi từ Order aggregate
  /// </summary>
  internal void UpdateQuantity(int newQuantity)
  {
    Quantity = Guard.Against.NegativeOrZero(newQuantity);
  }

  internal void ApplyDiscount(decimal discount)
  {
    Discount = Guard.Against.Negative(discount);

    if (Discount > UnitPrice)
    {
      throw new DomainException("Discount cannot exceed unit price");
    }
  }
}
