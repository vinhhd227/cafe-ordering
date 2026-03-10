using Api.Core.Aggregates.PromotionAggregate.Events;

namespace Api.Core.Aggregates.PromotionAggregate;

/// <summary>
///   Aggregate root đại diện cho một chương trình khuyến mãi.
///   Hỗ trợ: giảm %, giảm tiền cố định, mua X tặng Y.
///   Phạm vi: toàn order, sản phẩm cụ thể, hoặc danh mục sản phẩm.
/// </summary>
public class Promotion : SoftDeletableEntity<int>, IAggregateRoot
{
  private Promotion() { }

  public string Name { get; private set; } = string.Empty;

  /// <summary>Mã khuyến mãi. Null = không cần nhập code, tự động áp dụng.</summary>
  public string? Code { get; private set; }

  public string? Description { get; private set; }

  public DiscountType DiscountType { get; private set; } = DiscountType.Percentage;

  /// <summary>
  ///   Giá trị discount:
  ///   - PERCENTAGE: phần trăm (0–100)
  ///   - FIXED: số tiền (VND)
  ///   - BUY_X_GET_Y: không dùng (= 0)
  /// </summary>
  public decimal DiscountValue { get; private set; }

  /// <summary>Số lượng cần mua. Chỉ dùng khi DiscountType = BUY_X_GET_Y.</summary>
  public int? BuyQuantity { get; private set; }

  /// <summary>Số lượng được tặng. Chỉ dùng khi DiscountType = BUY_X_GET_Y.</summary>
  public int? GetQuantity { get; private set; }

  public PromotionScope Scope { get; private set; } = PromotionScope.Order;

  /// <summary>Danh sách ProductId áp dụng. Chỉ dùng khi Scope = PRODUCT. Lưu dạng JSON.</summary>
  public List<int> ApplicableProductIds { get; private set; } = new();

  /// <summary>Danh sách CategoryId áp dụng. Chỉ dùng khi Scope = CATEGORY. Lưu dạng JSON.</summary>
  public List<int> ApplicableCategoryIds { get; private set; } = new();

  public StackPolicy StackPolicy { get; private set; } = StackPolicy.Exclusive;

  /// <summary>Tổng tiền tối thiểu của order để áp dụng. Null = không yêu cầu.</summary>
  public decimal? MinOrderAmount { get; private set; }

  public DateTime StartDate { get; private set; }

  /// <summary>Ngày hết hạn. Null = không hết hạn.</summary>
  public DateTime? EndDate { get; private set; }

  /// <summary>Giới hạn tổng số lần sử dụng. Null = không giới hạn.</summary>
  public int? MaxUsage { get; private set; }

  public int CurrentUsage { get; private set; }

  public bool IsActive { get; private set; } = true;

  // ── Factory ─────────────────────────────────────────────────────

  public static Promotion Create(
    string name,
    string? code,
    string? description,
    DiscountType discountType,
    decimal discountValue,
    int? buyQuantity,
    int? getQuantity,
    PromotionScope scope,
    List<int>? applicableProductIds,
    List<int>? applicableCategoryIds,
    StackPolicy stackPolicy,
    decimal? minOrderAmount,
    DateTime startDate,
    DateTime? endDate,
    int? maxUsage)
  {
    Guard.Against.NullOrWhiteSpace(name);

    if (discountType == DiscountType.Percentage)
      Guard.Against.OutOfRange(discountValue, nameof(discountValue), 0, 100);
    else if (discountType == DiscountType.Fixed)
      Guard.Against.Negative(discountValue);
    else if (discountType == DiscountType.BuyXGetY)
    {
      Guard.Against.Null(buyQuantity, nameof(buyQuantity));
      Guard.Against.Null(getQuantity, nameof(getQuantity));
      Guard.Against.NegativeOrZero(buyQuantity!.Value, nameof(buyQuantity));
      Guard.Against.NegativeOrZero(getQuantity!.Value, nameof(getQuantity));
    }

    var promo = new Promotion
    {
      Name                   = name.Trim(),
      Code                   = code?.Trim().ToUpperInvariant() is { Length: > 0 } c ? c : null,
      Description            = description?.Trim() is { Length: > 0 } d ? d : null,
      DiscountType           = discountType,
      DiscountValue          = discountValue,
      BuyQuantity            = buyQuantity,
      GetQuantity            = getQuantity,
      Scope                  = scope,
      ApplicableProductIds   = applicableProductIds  ?? new(),
      ApplicableCategoryIds  = applicableCategoryIds ?? new(),
      StackPolicy            = stackPolicy,
      MinOrderAmount         = minOrderAmount,
      StartDate              = startDate,
      EndDate                = endDate,
      MaxUsage               = maxUsage,
      CurrentUsage           = 0,
      IsActive               = true,
    };

    return promo;
  }

  // ── Behaviors ───────────────────────────────────────────────────

  public void Activate()   => IsActive = true;
  public void Deactivate() => IsActive = false;

  public void IncrementUsage(int orderId)
  {
    CurrentUsage++;
    RegisterDomainEvent(new PromotionUsedEvent(this, orderId));
  }

  public void Update(
    string name,
    string? code,
    string? description,
    DiscountType discountType,
    decimal discountValue,
    int? buyQuantity,
    int? getQuantity,
    PromotionScope scope,
    List<int>? applicableProductIds,
    List<int>? applicableCategoryIds,
    StackPolicy stackPolicy,
    decimal? minOrderAmount,
    DateTime startDate,
    DateTime? endDate,
    int? maxUsage)
  {
    Guard.Against.NullOrWhiteSpace(name);
    Name                  = name.Trim();
    Code                  = code?.Trim().ToUpperInvariant() is { Length: > 0 } c ? c : null;
    Description           = description?.Trim() is { Length: > 0 } d ? d : null;
    DiscountType          = discountType;
    DiscountValue         = discountValue;
    BuyQuantity           = buyQuantity;
    GetQuantity           = getQuantity;
    Scope                 = scope;
    ApplicableProductIds  = applicableProductIds  ?? new();
    ApplicableCategoryIds = applicableCategoryIds ?? new();
    StackPolicy           = stackPolicy;
    MinOrderAmount        = minOrderAmount;
    StartDate             = startDate;
    EndDate               = endDate;
    MaxUsage              = maxUsage;
  }

  // ── Validation helpers ───────────────────────────────────────────

  public bool IsValidAt(DateTime utcNow)
    => IsActive && !IsDeleted
       && utcNow >= StartDate
       && (EndDate == null || utcNow <= EndDate.Value);

  public bool HasUsageLeft()
    => MaxUsage == null || CurrentUsage < MaxUsage.Value;

  public bool IsApplicableTo(decimal orderAmount)
    => MinOrderAmount == null || orderAmount >= MinOrderAmount.Value;
}
