namespace Api.Core.Aggregates.ProductAggregate.Events;

/// <summary>
///   Event khi Product details được cập nhật
/// </summary>
public class ProductUpdatedEvent : DomainEventBase
{
  public ProductUpdatedEvent(Product product)
  {
    ProductId = product.Id;
    ProductName = product.Name;
  }

  public int ProductId { get; }
  public string ProductName { get; }
}
