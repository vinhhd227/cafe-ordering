using Api.Core.Aggregates.CategoryAggregate.Events;

namespace Api.Core.Aggregates.CategoryAggregate;

public class Category : SoftDeletableEntity<int>, IAggregateRoot
{
  private Category() { }
  public string Name { get; private set; } = string.Empty;
  public string? Description { get; private set; }
  public bool IsActive { get; private set; } = true;

  public static Category Create(string name, string? description = null)
  {
    var category = new Category
    {
      Name = Guard.Against.NullOrEmpty(name),
      Description = description?.Trim() is { Length: > 0 } d ? d : null,
      IsActive = true
    };

    category.RegisterDomainEvent(new CategoryCreatedEvent(category));

    return category;
  }

  public void Update(string name, string? description)
  {
    Name = Guard.Against.NullOrEmpty(name);
    Description = description?.Trim() is { Length: > 0 } d ? d : null;

    RegisterDomainEvent(new CategoryUpdatedEvent(this));
  }

  public void Activate()
  {
    IsActive = true;

    RegisterDomainEvent(new CategoryActivatedEvent(Id));
  }

  public void Deactivate()
  {
    IsActive = false;

    RegisterDomainEvent(new CategoryDeactivatedEvent(Id));
  }
}
