using System.Text.Json;
using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Events;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Orders.DTOs;
using Api.UseCases.Orders.Interfaces;
using Mediator;

namespace Api.UseCases.Orders.EventHandlers;

/// <summary>
///   Helper dùng chung: map Order entity → OrderDto (cần resolve TableCode từ session).
/// </summary>
file static class OrderDtoMapper
{
  private static readonly JsonSerializerOptions JsonOpts = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  public static async Task<string> ToJsonAsync(
    Order order,
    IReadRepositoryBase<GuestSession> sessionRepo,
    IReadRepositoryBase<Table> tableRepo,
    CancellationToken ct)
  {
    string? tableCode = null;
    var session = await sessionRepo.FirstOrDefaultAsync(new SessionByIdSpec(order.SessionId), ct);
    if (session?.TableId.HasValue == true)
    {
      var table = await tableRepo.GetByIdAsync(session.TableId.Value, ct);
      tableCode = table?.Code;
    }

    var dto = new OrderDto(
      order.Id,
      order.OrderNumber,
      order.Status.Name.ToUpperInvariant(),
      order.PaymentStatus.Name.ToUpperInvariant(),
      order.PaymentMethod.Name.ToUpperInvariant(),
      order.AmountReceived,
      order.TipAmount,
      order.TotalAmount,
      order.OrderDate,
      order.SessionId,
      tableCode,
      order.Items.Select(i => new OrderItemDto(
        i.ProductId,
        i.ProductName,
        i.UnitPrice,
        i.Quantity,
        i.TotalPrice,
        i.Temperature?.Name.ToUpperInvariant(),
        i.IceLevel?.Name.ToUpperInvariant(),
        i.SugarLevel?.Name.ToUpperInvariant(),
        i.IsTakeaway
      )).ToList()
    );

    return JsonSerializer.Serialize(dto, JsonOpts);
  }
}

// ── OrderCreatedEvent ─────────────────────────────────────────────

public class NotifyOnOrderCreated(
  IOrderSseNotifier notifier,
  IReadRepositoryBase<GuestSession> sessionRepo,
  IReadRepositoryBase<Table> tableRepo)
  : INotificationHandler<OrderCreatedEvent>
{
  public async ValueTask Handle(OrderCreatedEvent notification, CancellationToken ct)
  {
    var json = await OrderDtoMapper.ToJsonAsync(notification.Order, sessionRepo, tableRepo, ct);
    await notifier.BroadcastAsync("order_created", json, ct);
  }
}

// ── OrderStatusChangedEvent ───────────────────────────────────────

public class NotifyOnOrderStatusChanged(
  IOrderSseNotifier notifier,
  IReadRepositoryBase<GuestSession> sessionRepo,
  IReadRepositoryBase<Table> tableRepo)
  : INotificationHandler<OrderStatusChangedEvent>
{
  public async ValueTask Handle(OrderStatusChangedEvent notification, CancellationToken ct)
  {
    var json = await OrderDtoMapper.ToJsonAsync(notification.Order, sessionRepo, tableRepo, ct);
    await notifier.BroadcastAsync("order_updated", json, ct);
  }
}

// ── OrderPaymentUpdatedEvent ──────────────────────────────────────

public class NotifyOnOrderPaymentUpdated(
  IOrderSseNotifier notifier,
  IReadRepositoryBase<GuestSession> sessionRepo,
  IReadRepositoryBase<Table> tableRepo)
  : INotificationHandler<OrderPaymentUpdatedEvent>
{
  public async ValueTask Handle(OrderPaymentUpdatedEvent notification, CancellationToken ct)
  {
    var json = await OrderDtoMapper.ToJsonAsync(notification.Order, sessionRepo, tableRepo, ct);
    await notifier.BroadcastAsync("order_updated", json, ct);
  }
}
