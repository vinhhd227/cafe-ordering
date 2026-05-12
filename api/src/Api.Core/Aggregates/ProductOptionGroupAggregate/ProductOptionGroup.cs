using Api.Core.Aggregates.ProductOptionGroupAggregate.Events;

namespace Api.Core.Aggregates.ProductOptionGroupAggregate;

/// <summary>
///   Nhóm bán kèm (topping group) — standalone aggregate, reusable across products.
///   Gắn vào product qua ProductOptionGroupMapping.
/// </summary>
public class ProductOptionGroup : SoftDeletableEntity<int>, IAggregateRoot
{
  private readonly List<ProductOptionValue> _values = new();
  private readonly List<ProductOptionGroupMapping> _mappings = new();

  private ProductOptionGroup() { }

  public string Name { get; private set; } = string.Empty;
  public bool IsRequired { get; private set; }
  public bool AllowMultiple { get; private set; }
  public bool AllowQuantity { get; private set; }
  public bool IsActive { get; private set; } = true;
  public int DisplayOrder { get; private set; }

  public IReadOnlyCollection<ProductOptionValue> Values => _values.AsReadOnly();
  public IReadOnlyCollection<ProductOptionGroupMapping> Mappings => _mappings.AsReadOnly();

  public static ProductOptionGroup Create(
    string name,
    bool isRequired,
    bool allowMultiple,
    bool allowQuantity)
  {
    var group = new ProductOptionGroup
    {
      Name = Guard.Against.NullOrWhiteSpace(name),
      IsRequired = isRequired,
      AllowMultiple = allowMultiple,
      AllowQuantity = allowQuantity,
    };

    group.RegisterDomainEvent(new ProductOptionGroupCreatedEvent(group));

    return group;
  }

  public void Update(string name, bool isRequired, bool allowMultiple, bool allowQuantity)
  {
    Name = Guard.Against.NullOrWhiteSpace(name);
    IsRequired = isRequired;
    AllowMultiple = allowMultiple;
    AllowQuantity = allowQuantity;

    RegisterDomainEvent(new ProductOptionGroupUpdatedEvent(this));
  }

  public void Activate()
  {
    IsActive = true;
    RegisterDomainEvent(new ProductOptionGroupUpdatedEvent(this));
  }

  public void Deactivate()
  {
    IsActive = false;
    RegisterDomainEvent(new ProductOptionGroupUpdatedEvent(this));
  }

  public void ToggleActive()
  {
    IsActive = !IsActive;
    RegisterDomainEvent(new ProductOptionGroupUpdatedEvent(this));
  }

  public bool ToggleValueStock(int valueId)
  {
    var value = _values.FirstOrDefault(v => v.Id == valueId);
    if (value is null) return false;
    value.SetStock(!value.IsInStock);
    RegisterDomainEvent(new ProductOptionGroupUpdatedEvent(this));
    return true;
  }

  /// <summary>Xóa toàn bộ values và thay bằng danh sách mới.</summary>
  public void ReplaceValues(IReadOnlyList<ProductOptionValueData> values)
  {
    _values.Clear();

    for (var i = 0; i < values.Count; i++)
    {
      var v = values[i];
      _values.Add(ProductOptionValue.Create(v.Name, v.Price, v.CostPrice, i));
    }

    RegisterDomainEvent(new ProductOptionGroupUpdatedEvent(this));
  }
}

/// <summary>Data record dùng khi tạo/thay thế values.</summary>
public record ProductOptionValueData(
  string Name,
  decimal Price,
  decimal? CostPrice);
