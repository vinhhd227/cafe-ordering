namespace Api.Core.Aggregates.ProductOptionGroupAggregate.Events;

public class ProductOptionGroupUpdatedEvent(ProductOptionGroup group) : DomainEventBase
{
  public ProductOptionGroup Group { get; } = group;
}
