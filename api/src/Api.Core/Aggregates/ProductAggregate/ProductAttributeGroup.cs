namespace Api.Core.Aggregates.ProductAggregate;

public enum OptionSelectionType { Single = 1, Multiple = 2 }

public class ProductAttributeGroup : BaseEntity
{
  private readonly List<ProductAttributeValue> _values = new();

  private ProductAttributeGroup() { }

  public int ProductId { get; private set; }
  public string Name { get; private set; } = string.Empty;
  public bool IsRequired { get; private set; }
  public OptionSelectionType SelectionType { get; private set; }
  public int DisplayOrder { get; private set; }

  public IReadOnlyCollection<ProductAttributeValue> Values => _values.AsReadOnly();

  internal static ProductAttributeGroup Create(
    int productId,
    string name,
    bool isRequired,
    OptionSelectionType selectionType,
    int displayOrder)
  {
    return new ProductAttributeGroup
    {
      ProductId = productId,
      Name = Guard.Against.NullOrEmpty(name),
      IsRequired = isRequired,
      SelectionType = selectionType,
      DisplayOrder = displayOrder
    };
  }

  internal void AddValue(string label, decimal priceAdjustment, bool isDefault, int displayOrder)
  {
    _values.Add(ProductAttributeValue.Create(Id, label, priceAdjustment, isDefault, displayOrder));
  }
}
