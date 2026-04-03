using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate.Events;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.Core.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Api.UseCases.Orders.EventHandlers;

/// <summary>
/// Gửi web push notification khi có đơn mới hoặc đơn thay đổi trạng thái.
/// </summary>
public class PushOnOrderCreated(
    IPushNotificationService push,
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

            await push.SendToAllAsync(
                title: $"Đơn mới #{order.OrderNumber}",
                body: body,
                url: $"/orders/{order.Id}",
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
    IPushNotificationService push,
    ILogger<PushOnOrderStatusChanged> logger)
    : INotificationHandler<OrderStatusChangedEvent>
{
    public async ValueTask Handle(OrderStatusChangedEvent notification, CancellationToken ct)
    {
        try
        {
            var order = notification.Order;

            // Chỉ push khi đơn hoàn thành hoặc bị hủy (status đáng quan tâm)
            if (order.Status.Name is not ("COMPLETED" or "CANCELLED")) return;

            var statusLabel = order.Status.Name == "COMPLETED" ? "Hoàn thành" : "Đã hủy";

            await push.SendToAllAsync(
                title: $"Đơn #{order.OrderNumber} — {statusLabel}",
                body: $"Tổng: {order.FinalAmount:N0}đ",
                url: $"/orders/{order.Id}",
                ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send push for OrderStatusChangedEvent {OrderId}", notification.Order.Id);
        }
    }
}
