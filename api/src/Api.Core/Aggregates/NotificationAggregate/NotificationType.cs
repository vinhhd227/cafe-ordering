namespace Api.Core.Aggregates.NotificationAggregate;

public class NotificationType : SmartEnum<NotificationType>
{
    public static readonly NotificationType OrderCreated       = new("ORDER_CREATED",        1);
    public static readonly NotificationType OrderCancelled     = new("ORDER_CANCELLED",      2);
    public static readonly NotificationType OrderCompleted     = new("ORDER_COMPLETED",      3);
    public static readonly NotificationType PaymentReceived    = new("PAYMENT_RECEIVED",     4);
    public static readonly NotificationType ManualOrderCreated = new("MANUAL_ORDER_CREATED", 5);
    public static readonly NotificationType LowStock           = new("LOW_STOCK",            6);
    public static readonly NotificationType SystemAlert        = new("SYSTEM_ALERT",         7);

    private NotificationType(string name, int value) : base(name, value) { }
}
