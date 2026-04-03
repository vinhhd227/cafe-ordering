using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.CreateManual;

public class CreateManualOrderHandler(
  IRepositoryBase<Order> orderRepository,
  IRepositoryBase<GuestSession> sessionRepository,
  IReadRepositoryBase<Table> tableRepository,
  IReadRepositoryBase<Product> productRepository)
  : ICommandHandler<CreateManualOrderCommand, Result<OrderDto>>
{
  public async ValueTask<Result<OrderDto>> Handle(CreateManualOrderCommand request, CancellationToken ct)
  {
    // 1. Validate TableId tồn tại
    var table = await tableRepository.FirstOrDefaultAsync(new TableByIdSpec(request.TableId), ct);
    if (table is null)
      return Result.NotFound($"Table {request.TableId} not found.");

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

    // 4. Tạo GuestSession thủ công — không fire domain events, không ảnh hưởng Table.ActiveSessionId
    var session = GuestSession.CreateManual(request.TableId);
    await sessionRepository.AddAsync(session, ct);

    // 5. Tạo order — chưa có items, save để EF sinh Id
    var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}";
    var order = Order.Create(session.Id, orderNumber, guestCount: request.GuestCount, orderedAt: request.OrderedAt);
    await orderRepository.AddAsync(order, ct);

    // 6. Load products để validate và lấy giá từ DB
    var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
    var products = await productRepository.ListAsync(new ProductsByIdsSpec(productIds), ct);

    if (products.Count != productIds.Count)
    {
      var missing = productIds.Except(products.Select(p => p.Id)).First();
      return Result.NotFound($"Product {missing} not found.");
    }

    var productMap = products.ToDictionary(p => p.Id);

    // 7. Thêm items — bypass status guard
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

    // 8. Set status (bypass state machine nếu khác Pending)
    if (status != OrderStatus.Pending)
      order.ForceSetStatus(status);

    // 9. Cập nhật payment nếu có
    if (paymentStatus != PaymentStatus.Unpaid || paymentMethod != PaymentMethod.Unknown
        || request.AmountReceived.HasValue || request.TipAmount != 0)
    {
      order.UpdatePayment(paymentStatus, paymentMethod, request.AmountReceived, request.TipAmount);
    }

    // 10. Persist — không gọi NotifyCreated()
    await orderRepository.UpdateAsync(order, ct);

    // 11. Map → OrderDto
    var dto = MapToDto(order, table.Code, isManual: true);
    return Result.Success(dto);
  }

  private static OrderDto MapToDto(Order order, string? tableCode, bool isManual) => new(
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
