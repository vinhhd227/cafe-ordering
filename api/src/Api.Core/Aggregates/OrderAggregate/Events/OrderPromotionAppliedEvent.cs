namespace Api.Core.Aggregates.OrderAggregate.Events;

public class OrderPromotionAppliedEvent(Order order, int promotionId) : DomainEventBase
{
  public Order Order { get; } = order;
  public int PromotionId { get; } = promotionId;
}
