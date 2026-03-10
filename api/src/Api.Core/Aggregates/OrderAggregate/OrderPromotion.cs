using Api.Core.Aggregates.PromotionAggregate;

namespace Api.Core.Aggregates.OrderAggregate;

/// <summary>
///   Lưu thông tin khuyến mãi đã áp dụng vào một Order.
///   Là entity bên trong Order aggregate, không tồn tại độc lập.
/// </summary>
public class OrderPromotion : BaseEntity<int>
{
  private OrderPromotion() { }

  public int OrderId { get; private set; }
  public int PromotionId { get; private set; }

  /// <summary>Snapshot mã khuyến mãi tại thời điểm áp dụng (display only).</summary>
  public string PromoCode { get; private set; } = string.Empty;

  /// <summary>Số tiền đã giảm thực tế.</summary>
  public decimal DiscountAmount { get; private set; }

  /// <summary>Snapshot StackPolicy tại thời điểm áp dụng — dùng để enforce rule khi apply promo khác.</summary>
  public StackPolicy StackPolicy { get; private set; } = StackPolicy.Exclusive;

  internal static OrderPromotion Create(
    int orderId,
    int promotionId,
    string promoCode,
    decimal discountAmount,
    StackPolicy stackPolicy)
  {
    return new OrderPromotion
    {
      OrderId        = Guard.Against.NegativeOrZero(orderId),
      PromotionId    = Guard.Against.NegativeOrZero(promotionId),
      PromoCode      = promoCode ?? string.Empty,
      DiscountAmount = Guard.Against.Negative(discountAmount),
      StackPolicy    = stackPolicy,
    };
  }
}
