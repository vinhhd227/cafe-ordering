namespace Api.Core.Aggregates.OrderAggregate;

/// <summary>
///   Snapshot của một option value được chọn tại thời điểm đặt hàng.
///   Giá và label không thay đổi dù admin sửa option sau này.
/// </summary>
public class OrderItemOption : BaseEntity
{
  private OrderItemOption() { }

  public int OrderItemId { get; private set; }
  public int OptionValueId { get; private set; }
  public string GroupName { get; private set; } = string.Empty;
  public string Label { get; private set; } = string.Empty;
  public decimal PriceAdjustment { get; private set; }

  internal static OrderItemOption Create(
    int optionValueId,
    string groupName,
    string label,
    decimal priceAdjustment)
  {
    return new OrderItemOption
    {
      OptionValueId = optionValueId,
      GroupName = Guard.Against.NullOrEmpty(groupName),
      Label = Guard.Against.NullOrEmpty(label),
      PriceAdjustment = priceAdjustment
    };
  }
}
