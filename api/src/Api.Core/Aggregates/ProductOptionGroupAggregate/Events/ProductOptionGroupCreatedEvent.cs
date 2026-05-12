namespace Api.Core.Aggregates.ProductOptionGroupAggregate.Events;

public class ProductOptionGroupCreatedEvent(ProductOptionGroup group) : DomainEventBase
{
  public ProductOptionGroup Group { get; } = group;
}
