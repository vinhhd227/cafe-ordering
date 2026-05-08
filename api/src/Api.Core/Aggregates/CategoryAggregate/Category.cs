using Api.Core.Aggregates.CategoryAggregate.Events;

namespace Api.Core.Aggregates.CategoryAggregate;

public class Category : SoftDeletableEntity<int>, IAggregateRoot
{
  private Category() { }
  public string Name { get; private set; } = string.Empty;
  public string? Description { get; private set; }
  public string? ImageUrl { get; private set; }
  public int SortOrder { get; private set; }
  public bool IsActive { get; private set; } = true;

  public static Category Create(string name, string? description, string? imageUrl, int sortOrder)
  {
    var category = new Category
    {
      Name = Guard.Against.NullOrEmpty(name),
      Description = description?.Trim() is { Length: > 0 } d ? d : null,
      ImageUrl = imageUrl?.Trim() is { Length: > 0 } u ? u : null,
      SortOrder = sortOrder,
      IsActive = true
    };

    category.RegisterDomainEvent(new CategoryCreatedEvent(category));

    return category;
  }

  public void Update(string name, string? description, string? imageUrl)
  {
    Name = Guard.Against.NullOrEmpty(name);
    Description = description?.Trim() is { Length: > 0 } d ? d : null;
    ImageUrl = imageUrl?.Trim() is { Length: > 0 } u ? u : null;

    RegisterDomainEvent(new CategoryUpdatedEvent(this));
  }

  public void SetSortOrder(int order)
  {
    SortOrder = order;
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
