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

  /// <summary>Mã khuyến mãi. Bắt buộc — tự động sinh nếu không nhập.</summary>
  public string Code { get; private set; } = string.Empty;

  /// <summary>Phân loại hiển thị mã: PUBLIC (duyệt danh sách) / PRIVATE (nhập thủ công).</summary>
  public CodeVisibility CodeVisibility { get; private set; } = CodeVisibility.Public;

  public string? Description { get; private set; }

  public DiscountType DiscountType { get; private set; } = DiscountType.Percentage;

  /// <summary>
  ///   Giá trị discount:
  ///   - PERCENTAGE: phần trăm (0–100)
  ///   - FIXED: số tiền (VND)
  ///   - BUY_X_GET_Y: không dùng (= 0)
  /// </summary>
  public decimal DiscountValue { get; private set; }

  /// <summary>Giới hạn số tiền giảm tối đa. Chỉ áp dụng với PERCENTAGE. Null = không giới hạn.</summary>
  public decimal? MaxDiscountAmount { get; private set; }

  /// <summary>Số lượng cần mua. Chỉ dùng khi DiscountType = BUY_X_GET_Y.</summary>
  public int? BuyQuantity { get; private set; }

  /// <summary>Số lượng được tặng. Chỉ dùng khi DiscountType = BUY_X_GET_Y.</summary>
  public int? GetQuantity { get; private set; }

  public PromotionScope Scope { get; private set; } = PromotionScope.Order;

  /// <summary>Danh sách ProductId áp dụng. Chỉ dùng khi Scope = PRODUCT. Lưu dạng JSON.</summary>
  public List<int> ApplicableProductIds { get; private set; } = new();

  /// <summary>Danh sách CategoryId áp dụng. Chỉ dùng khi Scope = CATEGORY. Lưu dạng JSON.</summary>
  public List<int> ApplicableCategoryIds { get; private set; } = new();

  /// <summary>
  ///   Danh sách ProductId của item được tặng miễn phí (BUY_X_GET_Y cross-product).
  ///   Null = item tặng lấy từ chính scope.
  /// </summary>
  public List<int>? GetFromProductIds { get; private set; }

  /// <summary>
  ///   Danh sách CategoryId của item được tặng miễn phí (BUY_X_GET_Y cross-category).
  ///   Null = item tặng lấy từ chính scope.
  /// </summary>
  public List<int>? GetFromCategoryIds { get; private set; }

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
    string code,
    CodeVisibility codeVisibility,
    string? description,
    DiscountType discountType,
    decimal discountValue,
    decimal? maxDiscountAmount,
    int? buyQuantity,
    int? getQuantity,
    PromotionScope scope,
    List<int>? applicableProductIds,
    List<int>? applicableCategoryIds,
    List<int>? getFromProductIds,
    List<int>? getFromCategoryIds,
    decimal? minOrderAmount,
    DateTime startDate,
    DateTime? endDate,
    int? maxUsage)
  {
    Guard.Against.NullOrWhiteSpace(name);
    Guard.Against.NullOrWhiteSpace(code);

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

    return new Promotion
    {
      Name                  = name.Trim(),
      Code                  = code.Trim().ToUpperInvariant(),
      CodeVisibility        = codeVisibility,
      Description           = description?.Trim() is { Length: > 0 } d ? d : null,
      DiscountType          = discountType,
      DiscountValue         = discountValue,
      MaxDiscountAmount     = maxDiscountAmount > 0 ? maxDiscountAmount : null,
      BuyQuantity           = buyQuantity,
      GetQuantity           = getQuantity,
      Scope                 = scope,
      ApplicableProductIds  = applicableProductIds  ?? new(),
      ApplicableCategoryIds = applicableCategoryIds ?? new(),
      GetFromProductIds     = getFromProductIds  is { Count: > 0 } gp ? gp : null,
      GetFromCategoryIds    = getFromCategoryIds is { Count: > 0 } gc ? gc : null,
      MinOrderAmount        = minOrderAmount,
      StartDate             = startDate,
      EndDate               = endDate,
      MaxUsage              = maxUsage,
      CurrentUsage          = 0,
      IsActive              = true,
    };
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
    string code,
    CodeVisibility codeVisibility,
    string? description,
    DiscountType discountType,
    decimal discountValue,
    decimal? maxDiscountAmount,
    int? buyQuantity,
    int? getQuantity,
    PromotionScope scope,
    List<int>? applicableProductIds,
    List<int>? applicableCategoryIds,
    List<int>? getFromProductIds,
    List<int>? getFromCategoryIds,
    decimal? minOrderAmount,
    DateTime startDate,
    DateTime? endDate,
    int? maxUsage)
  {
    Guard.Against.NullOrWhiteSpace(name);
    Guard.Against.NullOrWhiteSpace(code);
    Name                  = name.Trim();
    Code                  = code.Trim().ToUpperInvariant();
    CodeVisibility        = codeVisibility;
    Description           = description?.Trim() is { Length: > 0 } d ? d : null;
    DiscountType          = discountType;
    DiscountValue         = discountValue;
    MaxDiscountAmount     = maxDiscountAmount > 0 ? maxDiscountAmount : null;
    BuyQuantity           = buyQuantity;
    GetQuantity           = getQuantity;
    Scope                 = scope;
    ApplicableProductIds  = applicableProductIds  ?? new();
    ApplicableCategoryIds = applicableCategoryIds ?? new();
    GetFromProductIds     = getFromProductIds  is { Count: > 0 } gp ? gp : null;
    GetFromCategoryIds    = getFromCategoryIds is { Count: > 0 } gc ? gc : null;
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
