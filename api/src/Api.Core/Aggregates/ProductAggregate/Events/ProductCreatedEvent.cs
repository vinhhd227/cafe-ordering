namespace Api.Core.Aggregates.ProductAggregate.Events;

/// <summary>
///   Event khi Product được tạo mới
/// </summary>
public class ProductCreatedEvent : DomainEventBase
{
  public ProductCreatedEvent(Product product)
  {
    ProductId = product.Id;
    ProductName = product.Name;
    CategoryId = product.CategoryId;
  }

  public int ProductId { get; }
  public string ProductName { get; }
  public int CategoryId { get; }
}
