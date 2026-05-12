using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;
using Api.UseCases.Orders.DTOs;
using Api.UseCases.Promotions.Apply;
using Microsoft.Extensions.Configuration;

namespace Api.UseCases.Orders.Create;

/// <summary>
///   Tạo order mới trong một session đang active
/// </summary>
public class PlaceOrderHandler(
  IRepositoryBase<Order> orderRepository,
  IReadRepositoryBase<GuestSession> sessionRepository,
  IRepositoryBase<Promotion> promotionRepository,
  IReadRepositoryBase<Product> productRepository,
  IConfiguration configuration)
  : ICommandHandler<PlaceOrderCommand, Result<PlaceOrderResponseDto>>
{
  public async ValueTask<Result<PlaceOrderResponseDto>> Handle(
    PlaceOrderCommand request, CancellationToken ct)
  {
    // 1. Kiểm tra session tồn tại và còn active
    var sessionSpec = new SessionByIdSpec(request.SessionId);
    var session = await sessionRepository.FirstOrDefaultAsync(sessionSpec, ct);

    if (session is null)
      return Result.NotFound($"Session {request.SessionId} not found.");

    if (session.Status == GuestSessionStatus.Closed)
      return Result.Conflict("Cannot place order on a closed session.");

    // 2. Order cooldown — bỏ qua cho admin/staff (BypassCooldown = true)
    if (!request.BypassCooldown)
    {
      var cooldownSeconds = configuration.GetValue<int>("OrderCooldown:Seconds", 30);
      if (cooldownSeconds > 0)
      {
        var lastOrder = await orderRepository.FirstOrDefaultAsync(
          new LatestOrderBySessionIdSpec(request.SessionId), ct);

        if (lastOrder is not null)
        {
          var elapsed = DateTime.UtcNow - lastOrder.OrderDate;
          if (elapsed.TotalSeconds < cooldownSeconds)
          {
            var remaining = (int)(cooldownSeconds - elapsed.TotalSeconds) + 1;
            return Result.Invalid(new ValidationError("SessionId",
              $"Vui lòng chờ {remaining} giây trước khi đặt thêm."));
          }
        }
      }
    }

    // 3. Validate items
    if (request.Items is null || request.Items.Count == 0)
      return Result.Invalid(new ValidationError("Items", "Order must contain at least one item."));

    // 4. Load products kèm option groups/values để validate và build snapshots
    var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
    var products = await productRepository.ListAsync(new ProductsByIdsWithAttributesSpec(productIds), ct);
    var productMap = products.ToDictionary(p => p.Id);

    // 5. Tạo order — chưa có items, save để EF sinh Id
    var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}";
    var order = Order.Create(request.SessionId, orderNumber, guestCount: request.GuestCount);

    await orderRepository.AddAsync(order, ct);

    // 6. Thêm items với option snapshots
    foreach (var item in request.Items)
    {
      if (!productMap.TryGetValue(item.ProductId, out var product))
        return Result.NotFound($"Product {item.ProductId} not found.");

      var optionData = BuildOptionData(product, item.SelectedOptionValueIds);
      if (optionData is null)
        return Result.Invalid(new ValidationError("SelectedOptionValueIds",
          $"Invalid option value IDs for product '{product.Name}'."));

      var orderItem = order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity,
        optionData, item.IsTakeaway, item.IsFreeGift, item.Note);

      // Add ProductOptionGroup value selections (topping-style)
      if (item.SelectedOptionValues is { Count: > 0 })
      {
        foreach (var sel in item.SelectedOptionValues)
        {
          var mapping = product.OptionGroupMappings
            .FirstOrDefault(m => m.Group is not null && m.Group.Values.Any(v => v.Id == sel.OptionValueId));
          if (mapping?.Group is null) continue;

          var optValue = mapping.Group.Values.First(v => v.Id == sel.OptionValueId);
          orderItem.AddSelectedOptionValue(
            optValue.Id,
            mapping.Group.Name,
            optValue.Name,
            optValue.Price,
            sel.Quantity > 0 ? sel.Quantity : 1);
        }
      }
    }

    // 7. Đăng ký OrderCreatedEvent sau khi items đã được thêm
    order.NotifyCreated();
    await orderRepository.UpdateAsync(order, ct);

    // 8. Áp dụng promo nếu có (best-effort)
    if (!string.IsNullOrWhiteSpace(request.PromoCode))
      await TryApplyPromoAsync(order, request.PromoCode, ct);

    return Result.Success(new PlaceOrderResponseDto(order.Id, order.OrderNumber, order.TotalAmount));
  }

  /// <summary>
  ///   Dựng danh sách OrderItemOptionData từ selectedValueIds.
  ///   Trả null nếu có value ID không thuộc product này.
  /// </summary>
  private static IReadOnlyList<OrderItemOptionData>? BuildOptionData(
    Product product, List<int>? selectedValueIds)
  {
    if (selectedValueIds is null || selectedValueIds.Count == 0)
      return [];

    var result = new List<OrderItemOptionData>();

    foreach (var valueId in selectedValueIds)
    {
      var group = product.AttributeGroups
        .FirstOrDefault(g => g.Values.Any(v => v.Id == valueId));

      if (group is null) return null; // value ID không thuộc product này

      var value = group.Values.First(v => v.Id == valueId);
      result.Add(new OrderItemOptionData(value.Id, group.Name, value.Label, value.PriceAdjustment));
    }

    return result;
  }

  private async Task TryApplyPromoAsync(Order order, string code, CancellationToken ct)
  {
    try
    {
      var promo = await promotionRepository.FirstOrDefaultAsync(new PromotionByCodeSpec(code), ct);
      if (promo is null || !promo.IsValidAt(DateTime.UtcNow) || !promo.HasUsageLeft()) return;
      if (!promo.IsApplicableTo(order.TotalAmount)) return;

      Dictionary<int, int?>? productCategoryMap = null;
      var needsCategoryMap = (promo.Scope == PromotionScope.Category && promo.ApplicableCategoryIds.Any())
                          || (promo.GetFromCategoryIds is { Count: > 0 });
      if (needsCategoryMap)
      {
        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var products   = await productRepository.ListAsync(new ProductsByIdsSpec(productIds), ct);
        productCategoryMap = products.ToDictionary(p => p.Id, p => p.CategoryId);
      }

      IReadOnlyList<Product>? externalFreeProducts = null;
      if (promo.DiscountType == DiscountType.BuyXGetY && promo.GetFromProductIds is { Count: > 0 })
        externalFreeProducts = await productRepository.ListAsync(new ProductsByIdsSpec(promo.GetFromProductIds), ct);

      var discountResult = PromotionCalculator.Calculate(promo, order.Items, productCategoryMap, externalFreeProducts);
      if (discountResult.TotalDiscount <= 0) return;

      if (discountResult.FreeGifts is { Count: > 0 })
      {
        order.RemoveFreeGiftItems();
        foreach (var gift in discountResult.FreeGifts)
          order.AddItem(gift.ProductId, gift.ProductName, gift.UnitPrice, gift.Quantity, isFreeGift: true);
      }

      order.ApplyPromotion(promo.Id, promo.Code, discountResult.TotalDiscount);
      promo.IncrementUsage(order.Id);

      await orderRepository.UpdateAsync(order, ct);
      await promotionRepository.UpdateAsync(promo, ct);
    }
    catch
    {
      // Promo lỗi không được fail order — bỏ qua
    }
  }
}
