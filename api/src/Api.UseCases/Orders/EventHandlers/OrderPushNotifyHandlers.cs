using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.NotificationAggregate;
using Api.Core.Aggregates.OrderAggregate.Events;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.Core.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Api.UseCases.Orders.EventHandlers;

public class PushOnOrderCreated(
    INotificationService notifications,
    IReadRepositoryBase<GuestSession> sessionRepo,
    IReadRepositoryBase<Table> tableRepo,
    ILogger<PushOnOrderCreated> logger)
    : INotificationHandler<OrderCreatedEvent>
{
    public async ValueTask Handle(OrderCreatedEvent notification, CancellationToken ct)
    {
        try
        {
            var order = notification.Order;
            var tableCode = await ResolveTableCodeAsync(order.SessionId, ct);

            var body = tableCode is not null
                ? $"Bàn {tableCode} · {order.Items.Count} món"
                : $"{order.Items.Count} món";

            await notifications.SendAsync(
                NotificationType.OrderCreated,
                title: $"Đơn mới #{order.OrderNumber}",
                body: body,
                url: $"/orders/{order.Id}",
                referenceId: order.Id,
                ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send push for OrderCreatedEvent {OrderId}", notification.Order.Id);
        }
    }

    private async Task<string?> ResolveTableCodeAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await sessionRepo.FirstOrDefaultAsync(new SessionByIdSpec(sessionId), ct);
        if (session?.TableId is null) return null;
        var table = await tableRepo.GetByIdAsync(session.TableId.Value, ct);
        return table?.Code;
    }
}

public class PushOnOrderStatusChanged(
    INotificationService notifications,
    ILogger<PushOnOrderStatusChanged> logger)
    : INotificationHandler<OrderStatusChangedEvent>
{
    public async ValueTask Handle(OrderStatusChangedEvent notification, CancellationToken ct)
    {
        try
        {
            var order = notification.Order;

            if (order.Status.Name == "COMPLETED")
            {
                await notifications.SendAsync(
                    NotificationType.OrderCompleted,
                    title: $"Đơn #{order.OrderNumber} — Hoàn thành",
                    body: $"Tổng: {order.FinalAmount:N0}đ",
                    url: $"/orders/{order.Id}",
                    referenceId: order.Id,
                    ct: ct);
            }
            else if (order.Status.Name == "CANCELLED")
            {
                await notifications.SendAsync(
                    NotificationType.OrderCancelled,
                    title: $"Đơn #{order.OrderNumber} — Đã hủy",
                    body: $"Tổng: {order.FinalAmount:N0}đ",
                    url: $"/orders/{order.Id}",
                    referenceId: order.Id,
                    ct: ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send push for OrderStatusChangedEvent {OrderId}", notification.Order.Id);
        }
    }
}
