namespace Api.Core.Aggregates.PromotionAggregate;

public class DiscountType : SmartEnum<DiscountType>
{
  /// <summary>Giảm theo phần trăm. DiscountValue = phần trăm (0–100).</summary>
  public static readonly DiscountType Percentage = new PercentageType();

  /// <summary>Giảm số tiền cố định. DiscountValue = số tiền (VND).</summary>
  public static readonly DiscountType Fixed = new FixedType();

  /// <summary>Mua BuyQuantity tặng GetQuantity. Áp dụng cho item rẻ nhất trong scope.</summary>
  public static readonly DiscountType BuyXGetY = new BuyXGetYType();

  private DiscountType(string name, int value) : base(name, value) { }

  private sealed class PercentageType() : DiscountType("PERCENTAGE", 1) { }
  private sealed class FixedType()      : DiscountType("FIXED",      2) { }
  private sealed class BuyXGetYType()   : DiscountType("BUY_X_GET_Y", 3) { }
}
