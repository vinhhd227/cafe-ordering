namespace Api.Core.Aggregates.ProductAggregate;

public enum OptionSelectionType { Single = 1, Multiple = 2 }

public class ProductVariantGroup : BaseEntity
{
  private readonly List<ProductVariantValue> _values = new();

  private ProductVariantGroup() { }

  public int ProductId { get; private set; }
  public string Name { get; private set; } = string.Empty;
  public bool IsRequired { get; private set; }
  public OptionSelectionType SelectionType { get; private set; }
  public int DisplayOrder { get; private set; }

  public IReadOnlyCollection<ProductVariantValue> Values => _values.AsReadOnly();

  internal static ProductVariantGroup Create(
    int productId,
    string name,
    bool isRequired,
    OptionSelectionType selectionType,
    int displayOrder)
  {
    return new ProductVariantGroup
    {
      ProductId = productId,
      Name = Guard.Against.NullOrEmpty(name),
      IsRequired = isRequired,
      SelectionType = selectionType,
      DisplayOrder = displayOrder
    };
  }

  internal void AddValue(string label, decimal price, bool isDefault, int displayOrder)
  {
    _values.Add(ProductVariantValue.Create(Id, label, price, isDefault, displayOrder));
  }
}
