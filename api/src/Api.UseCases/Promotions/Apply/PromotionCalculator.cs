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
  /// <param name="productCategoryMap">productId → categoryId, cần thiết khi Scope = CATEGORY hoặc GetFromCategoryIds set.</param>
  public static DiscountResult Calculate(
    Promotion promo,
    IReadOnlyCollection<OrderItem> items,
    Dictionary<int, int>? productCategoryMap = null)
  {
    var scopedItems = GetScopedItems(promo, items, productCategoryMap);

    if (promo.DiscountType != DiscountType.BuyXGetY && scopedItems.Count == 0)
      return new DiscountResult(0, new());

    if (promo.DiscountType == DiscountType.Percentage)
      return CalculatePercentage(promo, scopedItems);

    if (promo.DiscountType == DiscountType.Fixed)
      return CalculateFixed(promo, items, scopedItems);

    if (promo.DiscountType == DiscountType.BuyXGetY)
      return CalculateBuyXGetY(promo, scopedItems, items, productCategoryMap);

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

    // Apply MaxDiscountAmount cap if set
    if (promo.MaxDiscountAmount.HasValue && totalDiscount > promo.MaxDiscountAmount.Value)
      totalDiscount = promo.MaxDiscountAmount.Value;

    return new DiscountResult(totalDiscount, itemDiscounts);
  }

  // ── Fixed amount ─────────────────────────────────────────────────

  private static DiscountResult CalculateFixed(
    Promotion promo,
    IReadOnlyCollection<OrderItem> allItems,
    List<OrderItem> scopedItems)
  {
    // Discount = min(discountValue, eligibleSubtotal) for all scopes
    var eligibleSubtotal = promo.Scope == PromotionScope.Order
      ? allItems.Sum(i => i.TotalPrice)
      : scopedItems.Sum(i => i.TotalPrice);

    var discount = Math.Min(promo.DiscountValue, eligibleSubtotal);
    return new DiscountResult(discount, new());
  }

  // ── Buy X Get Y ──────────────────────────────────────────────────

  private static DiscountResult CalculateBuyXGetY(
    Promotion promo,
    List<OrderItem> scopedItems,
    IReadOnlyCollection<OrderItem> allItems,
    Dictionary<int, int>? productCategoryMap)
  {
    if (promo.BuyQuantity == null || promo.GetQuantity == null)
      return new DiscountResult(0, new());

    var buyQty    = promo.BuyQuantity.Value;
    var getQty    = promo.GetQuantity.Value;
    var groupSize = buyQty + getQty;

    // Trigger: count qty of scoped items (buy side)
    var totalScopedQty = scopedItems.Sum(i => i.Quantity);
    if (totalScopedQty < buyQty)
      return new DiscountResult(0, new());

    var groups             = totalScopedQty / groupSize;
    var freeUnitsRemaining = groups > 0 ? groups * getQty : getQty;

    // Determine which pool of items can be given for free
    List<OrderItem> freePool;

    if (promo.GetFromProductIds is { Count: > 0 })
    {
      freePool = allItems
        .Where(i => promo.GetFromProductIds.Contains(i.ProductId))
        .ToList();
    }
    else if (promo.GetFromCategoryIds is { Count: > 0 } && productCategoryMap != null)
    {
      freePool = allItems
        .Where(i => productCategoryMap.TryGetValue(i.ProductId, out var catId)
                 && promo.GetFromCategoryIds.Contains(catId))
        .ToList();
    }
    else
    {
      // Default: free items come from scoped items themselves (cheapest)
      freePool = scopedItems;
    }

    if (freePool.Count == 0)
      return new DiscountResult(0, new());

    // Cheapest items free — sort by UnitPrice ASC
    var sortedPool    = freePool.OrderBy(i => i.UnitPrice).ToList();
    decimal totalDiscount = 0;

    foreach (var item in sortedPool)
    {
      if (freeUnitsRemaining <= 0) break;
      var freeFromItem    = Math.Min(item.Quantity, freeUnitsRemaining);
      totalDiscount      += freeFromItem * item.UnitPrice;
      freeUnitsRemaining -= freeFromItem;
    }

    return new DiscountResult(totalDiscount, new());
  }
}
