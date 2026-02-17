namespace Api.Core.Aggregates.CategoryAggregate.Events;

/// <summary>
///   Event khi Category bị vô hiệu hóa
/// </summary>
public class CategoryDeactivatedEvent(int categoryId) : DomainEventBase
{
  public int CategoryId { get; } = categoryId;
}
