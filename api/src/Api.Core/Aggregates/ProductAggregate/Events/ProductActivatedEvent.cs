namespace Api.Core.Aggregates.ProductAggregate.Events;

/// <summary>
///   Event khi Product được kích hoạt
/// </summary>
public class ProductActivatedEvent : DomainEventBase
{
  public ProductActivatedEvent(int productId)
  {
    ProductId = productId;
  }

  public int ProductId { get; }
}
