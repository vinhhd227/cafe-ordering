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

    // 4. Tạo order — chưa có items, save để EF sinh Id
    var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}";
    var order = Order.Create(request.SessionId, orderNumber, guestCount: request.GuestCount);

    await orderRepository.AddAsync(order, ct); // EF sinh order.Id sau bước này

    // 5. Thêm items (dùng order.Id đã được sinh)
    foreach (var item in request.Items)
    {
      DrinkTemperature? temp = item.Temperature is not null
        ? DrinkTemperature.FromName(NormalizeTemperature(item.Temperature), true) : null;
      IceLevel? iceLevel = item.IceLevel is not null
        ? IceLevel.FromName(NormalizeIceLevel(item.IceLevel), true) : null;
      SugarLevel? sugarLevel = item.SugarLevel is not null
        ? SugarLevel.FromName(NormalizeSugarLevel(item.SugarLevel), true) : null;

      order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity,
        temp, iceLevel, sugarLevel, item.IsTakeaway, item.IsFreeGift, item.Note);
    }

    // 6. Đăng ký OrderCreatedEvent sau khi items đã được thêm → SSE có đầy đủ data
    order.NotifyCreated();
    await orderRepository.UpdateAsync(order, ct); // Lưu items + dispatch event

    // 7. Áp dụng promo nếu có (best-effort — không fail order nếu promo lỗi)
    if (!string.IsNullOrWhiteSpace(request.PromoCode))
      await TryApplyPromoAsync(order, request.PromoCode, ct);

    return Result.Success(new PlaceOrderResponseDto(order.Id, order.OrderNumber, order.TotalAmount));
  }

  private async Task TryApplyPromoAsync(Order order, string code, CancellationToken ct)
  {
    try
    {
      var promo = await promotionRepository.FirstOrDefaultAsync(new PromotionByCodeSpec(code), ct);
      if (promo is null || !promo.IsValidAt(DateTime.UtcNow) || !promo.HasUsageLeft()) return;
      if (!promo.IsApplicableTo(order.TotalAmount)) return;

      Dictionary<int, int>? productCategoryMap = null;
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

  // Normalize legacy/alternative representations to canonical SmartEnum names.
  // Handles: integer strings ("1","2"), Vietnamese strings, already-correct names.
  private static string NormalizeTemperature(string raw) => raw.Trim() switch
  {
    "1" or "HOT"  or "Nóng"  or "nóng"  => "HOT",
    "2" or "COLD" or "Lạnh"  or "lạnh"  => "COLD",
    var s                                 => s.ToUpperInvariant()
  };

  private static string NormalizeIceLevel(string raw) => raw.Trim() switch
  {
    "1" or "LESS"   or "Ít đá"       or "ít đá"       => "LESS",
    "2" or "NORMAL" or "Bình thường"  or "bình thường" => "NORMAL",
    "3" or "MORE"   or "Nhiều đá"     or "nhiều đá"    => "MORE",
    var s                                                => s.ToUpperInvariant()
  };

  private static string NormalizeSugarLevel(string raw) => raw.Trim() switch
  {
    "1" or "LESS"   or "Ít đường"    or "ít đường"    => "LESS",
    "2" or "NORMAL" or "Bình thường" or "bình thường" => "NORMAL",
    "3" or "MORE"   or "Nhiều đường" or "nhiều đường" => "MORE",
    var s                                               => s.ToUpperInvariant()
  };
}
