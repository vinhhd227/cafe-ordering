namespace Api.Core.Aggregates.CategoryAggregate.Events;

/// <summary>
///   Event khi Category được cập nhật
/// </summary>
public class CategoryUpdatedEvent(Category category) : DomainEventBase
{
  public int CategoryId { get; } = category.Id;
  public string CategoryName { get; } = category.Name;
}
