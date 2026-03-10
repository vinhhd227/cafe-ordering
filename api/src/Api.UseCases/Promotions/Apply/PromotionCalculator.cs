using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.PromotionAggregate;

namespace Api.UseCases.Promotions.Apply;

/// <summary>
///   Kết quả tính toán discount. ItemDiscountsPerUnit chứa discount/unit cho từng sản phẩm
///   (chỉ có giá trị khi Scope = PRODUCT hoặc CATEGORY).
/// </summary>
public record DiscountResult(decimal TotalDiscount, Dictionary<int, decimal> ItemDiscountsPerUnit);

/// <summary>
///   Tính toán số tiền giảm giá từ một Promotion cho một Order.
///   Pure static — không ghi DB.
/// </summary>
public static class PromotionCalculator
{
  /// <param name="promo">Promotion đã được validate (active, valid, applicable).</param>
  /// <param name="items">Các OrderItem của order.</param>
  /// <param name="productCategoryMap">productId → categoryId, cần thiết khi Scope = CATEGORY.</param>
  public static DiscountResult Calculate(
    Promotion promo,
    IReadOnlyCollection<OrderItem> items,
    Dictionary<int, int>? productCategoryMap = null)
  {
    var scopedItems = GetScopedItems(promo, items, productCategoryMap);

    if (scopedItems.Count == 0)
      return new DiscountResult(0, new());

    if (promo.DiscountType == DiscountType.Percentage)
      return CalculatePercentage(promo, scopedItems);

    if (promo.DiscountType == DiscountType.Fixed)
      return CalculateFixed(promo, items, scopedItems);

    if (promo.DiscountType == DiscountType.BuyXGetY)
      return CalculateBuyXGetY(promo, scopedItems);

    return new DiscountResult(0, new());
  }

  // ── Scope filter ─────────────────────────────────────────────────

  private static List<OrderItem> GetScopedItems(
    Promotion promo,
    IReadOnlyCollection<OrderItem> items,
    Dictionary<int, int>? productCategoryMap)
  {
    if (promo.Scope == PromotionScope.Product)
      return items.Where(i => promo.ApplicableProductIds.Contains(i.ProductId)).ToList();

    if (promo.Scope == PromotionScope.Category)
      return items.Where(i =>
        productCategoryMap != null &&
        productCategoryMap.TryGetValue(i.ProductId, out var catId) &&
        promo.ApplicableCategoryIds.Contains(catId)).ToList();

    // ORDER scope — all items
    return items.ToList();
  }

  // ── Percentage ───────────────────────────────────────────────────

  private static DiscountResult CalculatePercentage(Promotion promo, List<OrderItem> scopedItems)
  {
    var itemDiscounts = new Dictionary<int, decimal>();
    decimal totalDiscount = 0;

    foreach (var item in scopedItems)
    {
      // Round to nearest 1 VND (no decimals for VND)
      var discountPerUnit = Math.Round(item.UnitPrice * promo.DiscountValue / 100, 0, MidpointRounding.AwayFromZero);
      itemDiscounts[item.ProductId] = discountPerUnit;
      totalDiscount += discountPerUnit * item.Quantity;
    }

    return new DiscountResult(totalDiscount, itemDiscounts);
  }

  // ── Fixed amount ─────────────────────────────────────────────────

  private static DiscountResult CalculateFixed(
    Promotion promo,
    IReadOnlyCollection<OrderItem> allItems,
    List<OrderItem> scopedItems)
  {
    if (promo.Scope == PromotionScope.Order)
    {
      var orderTotal = allItems.Sum(i => i.TotalPrice);
      var discount = Math.Min(promo.DiscountValue, orderTotal);
      return new DiscountResult(discount, new());
    }

    // Item-level fixed discount (per unit, capped at unit price)
    var itemDiscounts = new Dictionary<int, decimal>();
    decimal totalDiscount = 0;

    foreach (var item in scopedItems)
    {
      var discountPerUnit = Math.Min(promo.DiscountValue, item.UnitPrice);
      itemDiscounts[item.ProductId] = discountPerUnit;
      totalDiscount += discountPerUnit * item.Quantity;
    }

    return new DiscountResult(totalDiscount, itemDiscounts);
  }

  // ── Buy X Get Y ──────────────────────────────────────────────────

  private static DiscountResult CalculateBuyXGetY(Promotion promo, List<OrderItem> scopedItems)
  {
    if (promo.BuyQuantity == null || promo.GetQuantity == null)
      return new DiscountResult(0, new());

    var buyQty   = promo.BuyQuantity.Value;
    var getQty   = promo.GetQuantity.Value;
    var groupSize = buyQty + getQty;

    var totalScopedQty = scopedItems.Sum(i => i.Quantity);
    if (totalScopedQty < groupSize)
      return new DiscountResult(0, new());

    var groups            = totalScopedQty / groupSize;
    var freeUnitsRemaining = groups * getQty;

    // Cheapest items free — sort by UnitPrice ASC
    var sortedItems = scopedItems.OrderBy(i => i.UnitPrice).ToList();

    decimal totalDiscount = 0;

    foreach (var item in sortedItems)
    {
      if (freeUnitsRemaining <= 0) break;
      var freeFromItem = Math.Min(item.Quantity, freeUnitsRemaining);
      totalDiscount      += freeFromItem * item.UnitPrice;
      freeUnitsRemaining -= freeFromItem;
    }

    return new DiscountResult(totalDiscount, new());
  }
}
