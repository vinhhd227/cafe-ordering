using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.UpdateManual;

public class UpdateManualOrderHandler(
  IRepositoryBase<Order> orderRepository,
  IReadRepositoryBase<GuestSession> sessionRepository,
  IReadRepositoryBase<Table> tableRepository,
  IReadRepositoryBase<Product> productRepository)
  : ICommandHandler<UpdateManualOrderCommand, Result<OrderDto>>
{
  public async ValueTask<Result<OrderDto>> Handle(UpdateManualOrderCommand request, CancellationToken ct)
  {
    // 1. Load order với items + promotions
    var order = await orderRepository.FirstOrDefaultAsync(
      new OrderByIdWithItemsAndPromotionsSpec(request.OrderId), ct);

    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    // 2. Validate items không rỗng
    if (request.Items is null || request.Items.Count == 0)
      return Result.Invalid(new ValidationError("Items", "Order must contain at least one item."));

    // 3. Parse SmartEnums
    if (!OrderStatus.TryFromName(request.Status, true, out var status))
      return Result.Invalid(new ValidationError("Status", $"Invalid status: {request.Status}"));

    if (!PaymentStatus.TryFromName(request.PaymentStatus, true, out var paymentStatus))
      return Result.Invalid(new ValidationError("PaymentStatus", $"Invalid payment status: {request.PaymentStatus}"));

    if (!PaymentMethod.TryFromName(request.PaymentMethod, true, out var paymentMethod))
      return Result.Invalid(new ValidationError("PaymentMethod", $"Invalid payment method: {request.PaymentMethod}"));

    // 4. Load products để validate và lấy giá từ DB
    var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
    var products = await productRepository.ListAsync(new ProductsByIdsSpec(productIds), ct);

    if (products.Count != productIds.Count)
    {
      var missing = productIds.Except(products.Select(p => p.Id)).First();
      return Result.NotFound($"Product {missing} not found.");
    }

    var productMap = products.ToDictionary(p => p.Id);

    // 5. Xoá toàn bộ items + promotions, cập nhật OrderDate và GuestCount
    order.UpdateManually(request.OrderedAt, request.GuestCount);

    // 6. Re-add từng item với giá hiện tại từ DB
    foreach (var item in request.Items)
    {
      var product = productMap[item.ProductId];

      DrinkTemperature? temp = item.Temperature is not null
        ? DrinkTemperature.FromName(NormalizeTemperature(item.Temperature), true) : null;
      IceLevel? iceLevel = item.IceLevel is not null
        ? IceLevel.FromName(NormalizeIceLevel(item.IceLevel), true) : null;
      SugarLevel? sugarLevel = item.SugarLevel is not null
        ? SugarLevel.FromName(NormalizeSugarLevel(item.SugarLevel), true) : null;

      order.AddItemManual(item.ProductId, product.Name, product.Price, item.Quantity,
        temp, iceLevel, sugarLevel, item.IsTakeaway, item.Note);
    }

    // 7. Set status (bypass state machine)
    order.ForceSetStatus(status);

    // 8. Cập nhật payment
    order.UpdatePayment(paymentStatus, paymentMethod, request.AmountReceived, request.TipAmount);

    // 9. Persist
    await orderRepository.UpdateAsync(order, ct);

    // 10. Load session để lấy TableCode và IsManual
    string? tableCode = null;
    bool isManual = false;
    var session = await sessionRepository.FirstOrDefaultAsync(new SessionByIdSpec(order.SessionId), ct);
    if (session is not null)
    {
      isManual = session.Source == GuestSessionSource.Manual;
      if (session.TableId.HasValue)
      {
        var table = await tableRepository.FirstOrDefaultAsync(new TableByIdSpec(session.TableId.Value), ct);
        tableCode = table?.Code;
      }
    }

    // 11. Map → OrderDto
    var dto = new OrderDto(
      order.Id,
      order.OrderNumber,
      order.Status.Name.ToUpperInvariant(),
      order.PaymentStatus.Name.ToUpperInvariant(),
      order.PaymentMethod.Name.ToUpperInvariant(),
      order.AmountReceived,
      order.TipAmount,
      order.TotalAmount,
      order.TotalDiscount,
      order.FinalAmount,
      order.OrderDate,
      order.SessionId,
      tableCode,
      order.GuestCount,
      order.CompletedAt,
      order.PaidAt,
      order.Items.Select(i => new OrderItemDto(
        i.ProductId,
        i.ProductName,
        i.UnitPrice,
        i.Quantity,
        i.Discount,
        i.TotalPrice,
        i.Temperature?.Name.ToUpperInvariant(),
        i.IceLevel?.Name.ToUpperInvariant(),
        i.SugarLevel?.Name.ToUpperInvariant(),
        i.IsTakeaway,
        i.IsFreeGift,
        i.Note
      )).ToList(),
      order.Promotions.Select(p => new AppliedPromotionDto(p.PromotionId, p.PromoCode, p.DiscountAmount)).ToList(),
      isManual
    );

    return Result.Success(dto);
  }

  private static string NormalizeTemperature(string raw) => raw.Trim() switch
  {
    "1" or "HOT"  or "Nóng" or "nóng" => "HOT",
    "2" or "COLD" or "Lạnh" or "lạnh" => "COLD",
    var s                               => s.ToUpperInvariant()
  };

  private static string NormalizeIceLevel(string raw) => raw.Trim() switch
  {
    "1" or "LESS"   or "Ít đá"      or "ít đá"       => "LESS",
    "2" or "NORMAL" or "Bình thường" or "bình thường" => "NORMAL",
    "3" or "MORE"   or "Nhiều đá"   or "nhiều đá"    => "MORE",
    var s                                               => s.ToUpperInvariant()
  };

  private static string NormalizeSugarLevel(string raw) => raw.Trim() switch
  {
    "1" or "LESS"   or "Ít đường"   or "ít đường"   => "LESS",
    "2" or "NORMAL" or "Bình thường" or "bình thường" => "NORMAL",
    "3" or "MORE"   or "Nhiều đường" or "nhiều đường" => "MORE",
    var s                                               => s.ToUpperInvariant()
  };
}
