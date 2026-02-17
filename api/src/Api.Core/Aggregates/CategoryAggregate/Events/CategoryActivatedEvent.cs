namespace Api.Core.Aggregates.CategoryAggregate.Events;

/// <summary>
///   Event khi Category được kích hoạt
/// </summary>
public class CategoryActivatedEvent(int categoryId) : DomainEventBase
{
  public int CategoryId { get; } = categoryId;
}
