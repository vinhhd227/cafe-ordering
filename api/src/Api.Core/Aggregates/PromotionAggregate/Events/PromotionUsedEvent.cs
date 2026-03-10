namespace Api.Core.Aggregates.PromotionAggregate.Events;

public class PromotionUsedEvent(Promotion promotion, int orderId) : DomainEventBase
{
  public Promotion Promotion { get; } = promotion;
  public int OrderId { get; } = orderId;
}
