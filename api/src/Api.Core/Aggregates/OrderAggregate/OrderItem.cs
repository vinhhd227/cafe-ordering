using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Exceptions;

namespace Api.Core.Aggregates.OrderAggregate;

/// <summary>
///   OrderItem là entity BÊN TRONG Order aggregate
///   KHÔNG thể tồn tại độc lập, không phải Aggregate Root
/// </summary>
public class OrderItem : BaseEntity
{
  private readonly List<OrderItemOption> _selectedOptions = new();
  private readonly List<OrderItemSelectedOption> _selectedOptionValues = new();

  private OrderItem() { }

  public int OrderId { get; private set; }
  public int ProductId { get; private set; }
  public string ProductName { get; private set; } = string.Empty;
  public decimal UnitPrice { get; private set; }
  public int Quantity { get; private set; }
  public decimal Discount { get; private set; }

  /// <summary>Tổng price adjustment từ các variant option đã chọn (snapshot).</summary>
  public decimal OptionAdjustment { get; private set; }

  /// <summary>Tổng giá topping (option values) per item — denormalized để tính nhanh.</summary>
  public decimal OptionValueTotal { get; private set; }

  public bool IsTakeaway { get; private set; }
  public bool IsFreeGift { get; private set; }
  public string? Note { get; private set; }

  public IReadOnlyCollection<OrderItemOption> SelectedOptions => _selectedOptions.AsReadOnly();
  public IReadOnlyCollection<OrderItemSelectedOption> SelectedOptionValues => _selectedOptionValues.AsReadOnly();

  /// <summary>
  ///   TotalPrice = (UnitPrice + OptionAdjustment - Discount + OptionValueTotal) × Quantity
  ///   OptionValueTotal là tổng giá topping per 1 item instance.
  /// </summary>
  public decimal TotalPrice => (UnitPrice + OptionAdjustment - Discount + OptionValueTotal) * Quantity;

  internal static OrderItem Create(
    int orderId,
    int productId,
    string productName,
    decimal unitPrice,
    int quantity,
    bool isTakeaway = false,
    bool isFreeGift = false,
    string? note = null)
  {
    return new OrderItem
    {
      OrderId = Guard.Against.NegativeOrZero(orderId),
      ProductId = Guard.Against.NegativeOrZero(productId),
      ProductName = Guard.Against.NullOrEmpty(productName),
      UnitPrice = Guard.Against.Negative(unitPrice),
      Quantity = Guard.Against.NegativeOrZero(quantity),
      Discount = 0,
      IsTakeaway = isTakeaway,
      IsFreeGift = isFreeGift,
      Note = note,
    };
  }

  internal void AddOption(int optionValueId, string groupName, string label, decimal priceAdjustment)
  {
    _selectedOptions.Add(OrderItemOption.Create(optionValueId, groupName, label, priceAdjustment));
    OptionAdjustment += priceAdjustment;
  }

  public void AddSelectedOptionValue(int optionValueId, string groupName, string valueName, decimal unitPrice, int quantity)
  {
    _selectedOptionValues.Add(OrderItemSelectedOption.Create(optionValueId, groupName, valueName, unitPrice, quantity));
    OptionValueTotal += unitPrice * quantity;
  }

  internal void UpdateQuantity(int newQuantity)
  {
    if (IsFreeGift)
      throw new InvalidOperationException("Cannot change quantity of a free gift item.");
    Quantity = Guard.Against.NegativeOrZero(newQuantity);
  }

  internal void ApplyDiscount(decimal discount)
  {
    Discount = Guard.Against.Negative(discount);

    if (Discount > UnitPrice + OptionAdjustment)
      throw new DomainException("Discount cannot exceed unit price");
  }
}
