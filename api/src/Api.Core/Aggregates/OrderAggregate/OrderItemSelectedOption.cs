namespace Api.Core.Aggregates.OrderAggregate;

/// <summary>
///   Snapshot của một option value được chọn từ ProductOptionGroup tại thời điểm đặt hàng.
///   Giá và tên không đổi dù admin sửa option sau này.
/// </summary>
public class OrderItemSelectedOption : BaseEntity
{
  private OrderItemSelectedOption() { }

  public int OrderItemId { get; private set; }

  /// <summary>Ref đến ProductOptionValue gốc — không FK cứng để tránh cascade issue.</summary>
  public int OptionValueId { get; private set; }

  public string GroupName { get; private set; } = string.Empty;
  public string ValueName { get; private set; } = string.Empty;
  public decimal UnitPrice { get; private set; }
  public int Quantity { get; private set; }

  public decimal Subtotal => UnitPrice * Quantity;

  internal static OrderItemSelectedOption Create(
    int optionValueId,
    string groupName,
    string valueName,
    decimal unitPrice,
    int quantity)
  {
    return new OrderItemSelectedOption
    {
      OptionValueId = optionValueId,
      GroupName = Guard.Against.NullOrEmpty(groupName),
      ValueName = Guard.Against.NullOrEmpty(valueName),
      UnitPrice = Guard.Against.Negative(unitPrice),
      Quantity = Guard.Against.NegativeOrZero(quantity),
    };
  }
}
