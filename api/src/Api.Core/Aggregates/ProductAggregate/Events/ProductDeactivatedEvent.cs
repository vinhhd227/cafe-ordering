namespace Api.Core.Aggregates.ProductAggregate.Events;

/// <summary>
///   Event khi Product bị vô hiệu hóa
/// </summary>
public class ProductDeactivatedEvent : DomainEventBase
{
  public ProductDeactivatedEvent(int productId)
  {
    ProductId = productId;
  }

  public int ProductId { get; }
}
