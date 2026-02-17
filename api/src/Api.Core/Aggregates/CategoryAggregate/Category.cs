using Api.Core.Aggregates.CategoryAggregate.Events;

namespace Api.Core.Aggregates.CategoryAggregate;

public class Category : SoftDeletableEntity<int>, IAggregateRoot
{
  private Category() { }
  public string Name { get; private set; } = string.Empty;
  public bool IsActive { get; private set; } = true;

  public static Category Create(string name)
  {
    var category = new Category { Name = Guard.Against.NullOrEmpty(name), IsActive = true };

    category.RegisterDomainEvent(new CategoryCreatedEvent(category));

    return category;
  }

  public void UpdateName(string name)
  {
    Name = Guard.Against.NullOrEmpty(name);

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
