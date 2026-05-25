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

file static class OrderSseMapper
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
    if (order.SessionId.HasValue)
    {
      var session = await sessionRepo.FirstOrDefaultAsync(new SessionByIdSpec(order.SessionId.Value), ct);
      if (session?.TableId.HasValue == true)
      {
        var table = await tableRepo.GetByIdAsync(session.TableId.Value, ct);
        tableCode = table?.Code;
      }
    }

    var dto = order.ToOrderDto(tableCode, isManual: false);
    return JsonSerializer.Serialize(dto, JsonOpts);
  }
}

public class NotifyOnOrderCreated(
  IOrderSseNotifier notifier,
  IReadRepositoryBase<GuestSession> sessionRepo,
  IReadRepositoryBase<Table> tableRepo)
  : INotificationHandler<OrderCreatedEvent>
{
  public async ValueTask Handle(OrderCreatedEvent notification, CancellationToken ct)
  {
    var json = await OrderSseMapper.ToJsonAsync(notification.Order, sessionRepo, tableRepo, ct);
    await notifier.BroadcastAsync("order_created", json, ct);
  }
}

public class NotifyOnOrderStatusChanged(
  IOrderSseNotifier notifier,
  IReadRepositoryBase<GuestSession> sessionRepo,
  IReadRepositoryBase<Table> tableRepo)
  : INotificationHandler<OrderStatusChangedEvent>
{
  public async ValueTask Handle(OrderStatusChangedEvent notification, CancellationToken ct)
  {
    var json = await OrderSseMapper.ToJsonAsync(notification.Order, sessionRepo, tableRepo, ct);
    await notifier.BroadcastAsync("order_updated", json, ct);
  }
}

public class NotifyOnOrderPaymentUpdated(
  IOrderSseNotifier notifier,
  IReadRepositoryBase<GuestSession> sessionRepo,
  IReadRepositoryBase<Table> tableRepo)
  : INotificationHandler<OrderPaymentUpdatedEvent>
{
  public async ValueTask Handle(OrderPaymentUpdatedEvent notification, CancellationToken ct)
  {
    var json = await OrderSseMapper.ToJsonAsync(notification.Order, sessionRepo, tableRepo, ct);
    await notifier.BroadcastAsync("order_updated", json, ct);
  }
}
