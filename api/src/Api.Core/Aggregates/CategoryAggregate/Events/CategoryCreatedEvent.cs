namespace Api.Core.Aggregates.CategoryAggregate.Events;

/// <summary>
///   Event khi Category được tạo mới
/// </summary>
public class CategoryCreatedEvent(Category category) : DomainEventBase
{
  public int CategoryId { get; } = category.Id;
  public string CategoryName { get; } = category.Name;
}
