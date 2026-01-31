using api.Domain.Enums;

namespace api.Domain.Entities;

public class Order : AuditableEntity
{
    public int? TableId { get; set; }
    public Table? Table { get; set; }

    public string? CustomerName { get; set; }
    public string? Note { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Unknown;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
