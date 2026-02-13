using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.ProductAggregate.Events;

namespace Api.Core.Aggregates.ProductAggregate;

public class Product : SoftDeletableEntity<int>, IAggregateRoot
{
  private Product() { }
  public int CategoryId { get; private set; }
  public string Name { get; private set; } = string.Empty;
  public string? Description { get; private set; }
  public decimal Price { get; private set; }
  public bool IsActive { get; private set; } = true;
  public string? ImageUrl { get; private set; }
  public bool HasTemperatureOption { get; private set; }
  public bool HasIceLevelOption { get; private set; }
  public bool HasSugarLevelOption { get; private set; }

  // Navigation
  public Category? Category { get; private set; }

  public static Product Create(
    int categoryId,
    string name,
    decimal price,
    string? description = null,
    string? imageUrl = null,
    bool hasTemperatureOption = false,
    bool hasIceLevelOption = false,
    bool hasSugarLevelOption = false)
  {
    var product = new Product
    {
      CategoryId = Guard.Against.NegativeOrZero(categoryId),
      Name = Guard.Against.NullOrEmpty(name),
      Price = Guard.Against.NegativeOrZero(price),
      Description = description,
      ImageUrl = imageUrl,
      HasTemperatureOption = hasTemperatureOption,
      HasIceLevelOption = hasIceLevelOption,
      HasSugarLevelOption = hasSugarLevelOption,
      IsActive = true
    };

    product.RegisterDomainEvent(new ProductCreatedEvent(product));

    return product;
  }

  public void UpdateDetails(
    string name,
    decimal price,
    string? description = null,
    string? imageUrl = null)
  {
    Name = Guard.Against.NullOrEmpty(name);
    Price = Guard.Against.NegativeOrZero(price);
    Description = description;
    ImageUrl = imageUrl;

    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }

  public void UpdateOptions(
    bool hasTemperatureOption,
    bool hasIceLevelOption,
    bool hasSugarLevelOption)
  {
    HasTemperatureOption = hasTemperatureOption;
    HasIceLevelOption = hasIceLevelOption;
    HasSugarLevelOption = hasSugarLevelOption;

    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }

  public void ChangeCategory(int categoryId)
  {
    CategoryId = Guard.Against.NegativeOrZero(categoryId);

    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }

  public void Activate()
  {
    IsActive = true;

    RegisterDomainEvent(new ProductActivatedEvent(Id));
  }

  public void Deactivate()
  {
    IsActive = false;

    RegisterDomainEvent(new ProductDeactivatedEvent(Id));
  }
}
