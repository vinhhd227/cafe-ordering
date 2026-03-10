namespace Api.Core.Aggregates.PromotionAggregate;

public class PromotionScope : SmartEnum<PromotionScope>
{
  /// <summary>Áp dụng lên tổng tiền order.</summary>
  public static readonly PromotionScope Order = new OrderScope();

  /// <summary>Áp dụng lên các sản phẩm cụ thể (lọc theo ApplicableProductIds).</summary>
  public static readonly PromotionScope Product = new ProductScope();

  /// <summary>Áp dụng lên toàn bộ sản phẩm trong một danh mục (lọc theo ApplicableCategoryIds).</summary>
  public static readonly PromotionScope Category = new CategoryScope();

  private PromotionScope(string name, int value) : base(name, value) { }

  private sealed class OrderScope()    : PromotionScope("ORDER",    1) { }
  private sealed class ProductScope()  : PromotionScope("PRODUCT",  2) { }
  private sealed class CategoryScope() : PromotionScope("CATEGORY", 3) { }
}
