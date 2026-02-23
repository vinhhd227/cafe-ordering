using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.Create;

/// <summary>
///   Tạo order mới trong một session đang active
/// </summary>
public class PlaceOrderHandler(
  IRepositoryBase<Order> orderRepository,
  IReadRepositoryBase<GuestSession> sessionRepository)
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

    // 2. Validate items
    if (request.Items is null || request.Items.Count == 0)
      return Result.Invalid(new ValidationError("Items", "Order must contain at least one item."));

    // 3. Tạo order — chưa có items, save để EF sinh Id
    var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}";
    var order = Order.Create(request.SessionId, orderNumber);

    await orderRepository.AddAsync(order, ct); // EF sinh order.Id sau bước này

    // 4. Thêm items (dùng order.Id đã được sinh)
    foreach (var item in request.Items)
    {
      order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
    }

    await orderRepository.UpdateAsync(order, ct); // Lưu items

    return Result.Success(new PlaceOrderResponseDto(order.Id, order.OrderNumber, order.TotalAmount));
  }
}
